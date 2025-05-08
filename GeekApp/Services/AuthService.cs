using GeekApp.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using System.Net.Http;

namespace GeekApp.Services
{

    public class AuthService : IAuthService
    {
        private readonly HttpClient _http;
        private readonly JwtAuthStateProvider _authState;
        private readonly NavigationManager _navigation;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IHttpClientFactory factory,
            JwtAuthStateProvider authState,
            NavigationManager navigation,
            ILogger<AuthService> logger)
        {
            _http = factory.CreateClient();
            _authState = authState;
            _navigation = navigation;
            _logger = logger;
        }

        public async Task<bool> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogDebug("Sending login request for email: {Email}", request.Email);
                var response = await _http.PostAsJsonAsync("https://localhost:7282/api/auth/login", request);
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
                {
                    _logger.LogWarning("Invalid login response: Success={Success}, TokenIsEmpty={TokenIsEmpty}",
                        authResponse?.Success, string.IsNullOrEmpty(authResponse?.Token));
                    return false;
                }

                _logger.LogDebug("Login successful. Token received: {TokenPrefix}",
                    authResponse.Token.Substring(0, Math.Min(20, authResponse.Token.Length)));
                await _authState.MarkUserAsAuthenticated(authResponse.Token);
                var authState = await _authState.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("User logged in successfully with UserId: {UserId}",
                        authState.User.FindFirst("nameid")?.Value);
                    _navigation.NavigateTo("/", forceLoad: false);
                    return true;
                }
                _logger.LogWarning("Authentication state not updated after login.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for email: {Email}", request.Email);
                return false;
            }
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogDebug("Sending register request for email: {Email}", request.Email);
                var response = await _http.PostAsJsonAsync("https://localhost:7282/api/auth/register", request);
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
                {
                    _logger.LogWarning("Invalid register response: Success={Success}, TokenIsEmpty={TokenIsEmpty}",
                        authResponse?.Success, string.IsNullOrEmpty(authResponse?.Token));
                    return false;
                }

                _logger.LogDebug("Register successful. Token received: {TokenPrefix}",
                    authResponse.Token.Substring(0, Math.Min(20, authResponse.Token.Length)));
                await _authState.MarkUserAsAuthenticated(authResponse.Token);
                var authState = await _authState.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true)
                {
                    _logger.LogInformation("User registered successfully with UserId: {UserId}",
                        authState.User.FindFirst("nameid")?.Value);
                    _navigation.NavigateTo("/", forceLoad: false);
                    return true;
                }
                _logger.LogWarning("Authentication state not updated after register.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error for email: {Email}", request.Email);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogDebug("Initiating logout.");
                await _authState.MarkUserAsLoggedOut();
                _http.DefaultRequestHeaders.Authorization = null;
                _navigation.NavigateTo("/login", forceLoad: false);
                _logger.LogInformation("User logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error");
            }
        }
    }
}