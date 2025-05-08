using GeekApp.Shared.ApiModels;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace GeekApp.Services
{
    public interface IContentService
    {
        Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", int? year = null, string sortBy = null, int? pageSize = null);
        Task<TmdbResponse> GetTrendingAsync(string mediaType, string timeWindow, int page = 1, int? pageSize = null);
        Task<TmdbResponse> DiscoverAsync(string mediaType, int page, string sortBy, int? minYear, int? maxYear, List<int> genres, int? minVoteCount);
        Task<List<TmdbGenre>> GetGenresAsync(string mediaType);
        Task<TmdbRoot> GetTitleDetailsAsync(string mediaType, int tmdbId);
        Task<TmdbResult> GetTitleSummaryAsync(string mediaType, int id);
    }

    public class ContentService : IContentService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly ILogger<ContentService> _logger;

        public ContentService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ContentService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("AuthorizedAPI");
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7282";
            _logger = logger;
        }

        public async Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", int? year = null, string sortBy = null, int? pageSize = null)
        {
            try
            {
                var queryString = $"?query={Uri.EscapeDataString(query)}&page={page}&mediaType={mediaType}";
                if (!string.IsNullOrEmpty(sortBy))
                    queryString += $"&sortBy={Uri.EscapeDataString(sortBy)}";
                if (year.HasValue)
                    queryString += $"&year={year.Value}";
                if (pageSize.HasValue)
                    queryString += $"&pageSize={pageSize.Value}";

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tmdb/search{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TmdbSearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed for query: {Query}", query);
                throw;
            }
        }

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", int page = 1, int? pageSize = null)
        {
            try
            {
                var queryString = $"?mediaType={mediaType}&timeWindow={timeWindow}&page={page}";
                if (pageSize.HasValue)
                    queryString += $"&pageSize={pageSize.Value}";

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tmdb/trending{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TmdbResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get trending failed for mediaType: {MediaType}, timeWindow: {TimeWindow}", mediaType, timeWindow);
                throw;
            }
        }

        public async Task<TmdbResponse> DiscoverAsync(string mediaType, int page = 1, string sortBy = "popularity.desc", int? minYear = null, int? maxYear = null, List<int> genres = null, int? minVoteCount = 300)
        {
            try
            {
                var queryString = $"?mediaType={mediaType}&page={page}&sortBy={Uri.EscapeDataString(sortBy)}";
                if (minYear.HasValue)
                    queryString += $"&minYear={minYear.Value}";
                if (maxYear.HasValue)
                    queryString += $"&maxYear={maxYear.Value}";
                if (genres?.Count > 0)
                    queryString += $"&genres={Uri.EscapeDataString(string.Join(",", genres.OrderBy(g => g)))}";
                if (minVoteCount.HasValue)
                    queryString += $"&minVoteCount={minVoteCount.Value}";

                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tmdb/discover{queryString}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TmdbResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discover failed for mediaType: {MediaType}", mediaType);
                throw;
            }
        }

        public async Task<List<TmdbGenre>> GetGenresAsync(string mediaType)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tmdb/genres?mediaType={mediaType}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TmdbGenre>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get genres failed for mediaType: {MediaType}", mediaType);
                throw;
            }
        }

        public async Task<TmdbRoot> GetTitleDetailsAsync(string mediaType, int tmdbId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tmdb/title/{mediaType}/{tmdbId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TmdbRoot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch title details for {MediaType} {TmdbId}", mediaType, tmdbId);
                throw;
            }
        }

        public async Task<TmdbResult> GetTitleSummaryAsync(string mediaType, int id)
        {
            var response = await _httpClient.GetAsync($"api/tmdb/summary/{mediaType}/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TmdbResult>();
        }
    }
}