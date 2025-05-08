using GeekApp.Server.Models;
using GeekApp.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using GeekApp.Shared.Lists;

namespace GeekApp.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<ApiUser> _users;
        private readonly IMongoCollection<AddList> _lists;
        private readonly ILogger<AuthService> _logger;

        public AuthService(MongoDBService mongoDBService, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _mongoDBService = mongoDBService;
            _configuration = configuration;
            _users = _mongoDBService.Users;
            _lists = _mongoDBService.Lists;
            _logger = logger;
        }

        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            try
            {
                var existingUser = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    _logger.LogWarning("Registration attempt with existing email: {Email}", request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        ErrorMessage = "User already exists."
                    };
                }

                var user = new ApiUser
                {
                    UserId = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Email = request.Email,
                    CreatedAt = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(request.Password))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                await _users.InsertOneAsync(user);
                _logger.LogInformation("Registered user {Email} with UserId {UserId}", user.Email, user.UserId);

                // Create default watchlist
                var watchlist = new AddList
                {
                    UserId = user.UserId,
                    Name = "Watchlist",
                    IsWatchlist = true,
                    CreatedAt = DateTime.UtcNow,
                    Items = new List<ListItem>()
                };
                await _lists.InsertOneAsync(watchlist);
                _logger.LogInformation("Created watchlist for user {UserId}", user.UserId);

                var token = GenerateJwtToken(user);

                return new AuthResponse
                {
                    Success = true,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> Login(LoginRequest request)
        {
            try
            {
                var user = await _users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid login attempt for {Email}", request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid email or password."
                    };
                }

                var token = GenerateJwtToken(user);
                _logger.LogInformation("User {Email} logged in successfully", user.Email);

                return new AuthResponse
                {
                    Success = true,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = $"Login failed: {ex.Message}"
                };
            }
        }

        private string GenerateJwtToken(ApiUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new List<Claim>
            {
                new Claim("nameid", user.UserId),
                new Claim("email", user.Email),
                new Claim("unique_name", user.Name),
                new Claim("createdAt", user.CreatedAt.ToString("o"))
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}