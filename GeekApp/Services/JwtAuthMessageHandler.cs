using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GeekApp.Services
{
    public class JwtAuthMessageHandler : DelegatingHandler
    {
        private readonly JwtAuthStateProvider _authStateProvider;
        private readonly ILogger<JwtAuthMessageHandler> _logger;

        public JwtAuthMessageHandler(
            JwtAuthStateProvider authStateProvider,
            ILogger<JwtAuthMessageHandler> logger)
        {
            _authStateProvider = authStateProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Attempting to retrieve JWT token from session storage.");
                var token = await _authStateProvider.GetTokenAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("JWT token retrieved successfully: {TokenPrefix}",
                        token.Substring(0, Math.Min(20, token.Length)));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
                else
                {
                    _logger.LogDebug("No JWT token found in session storage for request to {Uri}", request.RequestUri);
                }

                _logger.LogDebug("Sending request to {Uri}", request.RequestUri);
                var response = await base.SendAsync(request, cancellationToken);
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process request to {Uri}", request.RequestUri);
                throw;
            }
        }
    }
}