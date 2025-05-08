using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GeekApp.Services
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly ILogger<JwtAuthStateProvider> _logger;
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
        private const string TokenKey = "authToken";
        private static string _cachedToken;

        public JwtAuthStateProvider(ProtectedSessionStorage sessionStorage, ILogger<JwtAuthStateProvider> logger)
        {
            _sessionStorage = sessionStorage;
            _logger = logger;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                _logger.LogDebug("Checking authentication state.");
                string token = _cachedToken;

                if (string.IsNullOrWhiteSpace(token))
                {
                    var result = await _sessionStorage.GetAsync<string>(TokenKey);
                    token = result.Success ? result.Value : null;
                    _logger.LogDebug("Session storage result: Success={Success}, Value={Value}",
                        result.Success, result.Success ? result.Value?.Substring(0, Math.Min(20, result.Value.Length)) : "null");
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogDebug("No token found in session storage or cache. Returning anonymous state.");
                    return new AuthenticationState(_anonymous);
                }

                _cachedToken = token;
                _logger.LogDebug("Token retrieved: {TokenPrefix}",
                    token.Substring(0, Math.Min(20, token.Length)));
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                _logger.LogInformation("Authentication state retrieved successfully for UserId: {UserId}",
                    claims.FirstOrDefault(c => c.Type == "nameid")?.Value);
                return new AuthenticationState(user);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                _logger.LogWarning("Cannot access session storage during prerendering. Using cached token: {TokenPrefix}",
                    _cachedToken?.Substring(0, Math.Min(20, _cachedToken?.Length ?? 0)) ?? "null");
                if (string.IsNullOrWhiteSpace(_cachedToken))
                {
                    return new AuthenticationState(_anonymous);
                }
                var claims = ParseClaimsFromJwt(_cachedToken);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                return new AuthenticationState(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get authentication state.");
                return new AuthenticationState(_anonymous);
            }
        }

        public async Task MarkUserAsAuthenticated(string token)
        {
            try
            {
                _logger.LogDebug("Storing JWT token in session storage: {TokenPrefix}",
                    token.Substring(0, Math.Min(20, token.Length)));
                _cachedToken = token;
                await _sessionStorage.SetAsync(TokenKey, token);

                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwt");
                var user = new ClaimsPrincipal(identity);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
                _logger.LogInformation("User marked as authenticated with UserId: {UserId}",
                    claims.FirstOrDefault(c => c.Type == "nameid")?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark user as authenticated.");
            }
        }

        public async Task MarkUserAsLoggedOut()
        {
            try
            {
                _logger.LogDebug("Clearing JWT token from session storage.");
                _cachedToken = null;
                await _sessionStorage.DeleteAsync(TokenKey);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
                _logger.LogInformation("User logged out successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark user as logged out.");
            }
        }

        public async Task<string> GetTokenAsync()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_cachedToken))
                {
                    _logger.LogDebug("Returning cached JWT token: {TokenPrefix}",
                        _cachedToken.Substring(0, Math.Min(20, _cachedToken.Length)));
                    return _cachedToken;
                }

                _logger.LogDebug("Retrieving JWT token from session storage.");
                var result = await _sessionStorage.GetAsync<string>(TokenKey);
                var token = result.Success ? result.Value : null;

                if (!string.IsNullOrEmpty(token))
                {
                    _cachedToken = token;
                    _logger.LogDebug("Token retrieved from session storage: {TokenPrefix}",
                        token.Substring(0, Math.Min(20, token.Length)));
                    return token;
                }
                _logger.LogWarning("No token found in session storage.");
                return null;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
            {
                _logger.LogWarning("Cannot access session storage during prerendering. Returning cached token: {TokenPrefix}",
                    _cachedToken?.Substring(0, Math.Min(20, _cachedToken?.Length ?? 0)) ?? "null");
                return _cachedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve JWT token.");
                return null;
            }
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                _logger.LogDebug("JWT claims parsed successfully. Claim count: {Count}", token.Claims.Count());
                return token.Claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse JWT claims.");
                return Enumerable.Empty<Claim>();
            }
        }
    }
}