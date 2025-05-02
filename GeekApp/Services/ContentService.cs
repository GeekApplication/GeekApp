using GeekApp.Shared.ApiModels;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GeekApp.Services
{
    public interface IContentService
    {
        Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", int? year = null, string sortBy = null, int? pageSize = null);
        Task<TmdbResponse> GetTrendingAsync(string mediaType, string timeWindow, int page = 1, int? pageSize = null);
        Task<TmdbResponse> DiscoverAsync(string mediaType, int page, string sortBy, int? minYear, int? maxYear, List<int> genres, int? minVoteCount);
        Task<List<TmdbGenre>> GetGenresAsync(string mediaType);
    }

    public class ContentService : IContentService
    {
        private readonly HttpClient _httpClient;

        public ContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", int? year = null, string sortBy = null, int? pageSize = null)
        {
            var queryString = $"?query={Uri.EscapeDataString(query)}&page={page}&mediaType={mediaType}";

            if (!string.IsNullOrEmpty(sortBy))
                queryString += $"&sortBy={Uri.EscapeDataString(sortBy)}";

            // Always pass year parameters regardless of mediaType
            if (year.HasValue)
                queryString += $"&year={year.Value}";
            if (pageSize.HasValue)
                queryString += $"&pageSize={pageSize.Value}";

            var response = await _httpClient.GetAsync($"https://localhost:7282/api/tmdb/search{queryString}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TmdbSearchResult>();
        }

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", int page = 1, int? pageSize = null)
        {
            var queryString = $"?mediaType={mediaType}&timeWindow={timeWindow}&page={page}";
            if (pageSize.HasValue)
                queryString += $"&pageSize={pageSize.Value}";

            var response = await _httpClient.GetAsync($"https://localhost:7282/api/tmdb/trending{queryString}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TmdbResponse>();
        }

        public async Task<TmdbResponse> DiscoverAsync(string mediaType, int page = 1, string sortBy = "popularity.desc", int? minYear = null, int? maxYear = null, List<int> genres = null, int? minVoteCount = 300)
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

            var response = await _httpClient.GetAsync($"https://localhost:7282/api/tmdb/discover{queryString}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TmdbResponse>();
        }

        public async Task<List<TmdbGenre>> GetGenresAsync(string mediaType)
        {
            var response = await _httpClient.GetAsync($"https://localhost:7282/api/tmdb/genres?mediaType={mediaType}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<TmdbGenre>>();
        }
    }
}