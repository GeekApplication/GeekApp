using System.Net.Http.Json;
using GeekApp.Server.Services;
using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Logging;

namespace GeekApp.Services;
public class BackendService
{
    private readonly HttpClient _http;
    private readonly ILogger<BackendService> _logger;
    private readonly ITmdbService _tmdbService;

    public BackendService(IHttpClientFactory httpClientFactory, ILogger<BackendService> logger, ITmdbService tmdbService)
    {
        _http = httpClientFactory.CreateClient("Backend");
        _tmdbService = tmdbService;
        _logger = logger;

        // Verify the base address is set
        if (_http.BaseAddress == null)
        {
            _logger.LogError("HttpClient BaseAddress is not configured!");
            throw new InvalidOperationException("HttpClient BaseAddress must be configured");
        }
        _logger.LogInformation("Using base address: {BaseAddress}", _http.BaseAddress);


    }

    public async Task<TmdbContentDetails?> GetMovieDetails(int movieId)
    {
        try
        {
            var uri = $"api/tmdb/details/movie/{movieId}"; // Matches controller route
            _logger.LogInformation("Fetching from: {Uri}", uri);

            var response = await _http.GetFromJsonAsync<TmdbContentDetails>(uri);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch movie details for {MovieId}", movieId);
            throw; // Rethrow to let the frontend handle the error
        }
    }

    public string GetImageUrl(string path, string size = "w500")
    {
        return _tmdbService.GetImageUrl(path, size);
    }

    public string GetBaseAddress()
    {
        return _http.BaseAddress?.ToString() ?? "[not configured]";
    }
}