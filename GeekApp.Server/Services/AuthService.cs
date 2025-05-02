using GeekApp.Server.Models;
using GeekApp.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GeekApp.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly MongoDBService _mongoDBService;
        private readonly IConfiguration _configuration;
        private readonly IMongoCollection<ApiUser> _users;

        public AuthService(MongoDBService mongoDBService, IConfiguration configuration)
        {
            _mongoDBService = mongoDBService;
            _configuration = configuration;
            _users = _mongoDBService.Users;
        }

        public async Task<AuthResponse> Register(RegisterRequest request)
        {
            var existingUser = await _mongoDBService.Users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "User already exists."
                };
            }

            var user = new ApiUser
            {
                Name = request.Name,
                Email = request.Email
            };

            if (!string.IsNullOrEmpty(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            await _mongoDBService.Users.InsertOneAsync(user);

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Token = token
            };
        }

        public async Task<AuthResponse> Login(LoginRequest request)
        {
            var user = await _mongoDBService.Users.Find(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse
                {
                    Success = false,
                    ErrorMessage = "Invalid email or password."
                };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponse
            {
                Success = true,
                Token = token
            };
        }

        private string GenerateJwtToken(ApiUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new List<Claim>
        {

            new Claim("nameid", user.Id),           
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
