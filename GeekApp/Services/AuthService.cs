using GeekApp.Services;
using GeekApp.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Headers;

namespace GeekApp.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _http;
    private readonly JwtAuthStateProvider _authState;
    private readonly NavigationManager _navigation;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(
        IHttpClientFactory factory,
        JwtAuthStateProvider authState,
        NavigationManager navigation,
        ILogger<AuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _http = factory.CreateClient("AuthorizedAPI");
        _authState = authState;
        _navigation = navigation;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/login", request);
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
            {
                _logger.LogWarning("Invalid login response: {@Response}", authResponse);
                return false;
            }

            // Save token to state
            await _authState.MarkUserAsAuthenticated(authResponse.Token);

            // Optionally set header manually
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.Token);

            _navigation.NavigateTo("/", forceLoad: true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return false;
        }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/auth/register", request);
            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();

            if (authResponse == null || !authResponse.Success || string.IsNullOrEmpty(authResponse.Token))
            {
                _logger.LogWarning("Invalid register response: {@Response}", authResponse);
                return false;
            }

            await _authState.MarkUserAsAuthenticated(authResponse.Token);
            _navigation.NavigateTo("/", true);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Register error");
            return false;
        }
    }


    public async Task LogoutAsync()
    {
        try
        {
            await _authState.MarkUserAsLoggedOut();
            _navigation.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
        }
    }

}