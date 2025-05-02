using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace GeekApp.Server.Services
{
    public class TmdbSettings
    {
        public string ApiKey { get; set; }
        public string BaseUrl { get; set; }
        public string ImageBaseUrl { get; set; }
    }

    public interface ITmdbService
    {
        Task<TmdbRoot> GetTitleDetailsAsync(string mediaType, int id);
        Task<TmdbResponse> GetTrendingAsync(string mediaType, string timeWindow);
        Task<TmdbContentDetails> GetContentDetailsAsync(string mediaType, int id);
        Task<TmdbCredits> GetCreditsAsync(string mediaType, int id);
        Task<TmdbResponse> GetSimilarAsync(string mediaType, int id);
        string GetImageUrl(string path, string size);
    }

    public class TmdbService : ITmdbService
    {
        private readonly HttpClient _httpClient;
        private readonly TmdbSettings _settings;

        public TmdbService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _settings = configuration.GetSection("Tmdb").Get<TmdbSettings>();
        }

        public async Task<TmdbRoot> GetTitleDetailsAsync(string mediaType, int id)
        {
            var root = new TmdbRoot();

            // Fetch details, credits, and similar titles concurrently
            var detailsTask = GetContentDetailsAsync(mediaType, id);
            var creditsTask = GetCreditsAsync(mediaType, id);
            var similarTask = GetSimilarAsync(mediaType, id);

            await Task.WhenAll(detailsTask, creditsTask, similarTask);

            root.Details = await detailsTask;
            root.Credits = await creditsTask;
            root.Similar = await similarTask;

            // Append image base URL
            if (!string.IsNullOrEmpty(root.Details.PosterPath))
                root.Details.PosterPath = GetImageUrl(root.Details.PosterPath, "w500");
            if (!string.IsNullOrEmpty(root.Details.BackdropPath))
                root.Details.BackdropPath = GetImageUrl(root.Details.BackdropPath, "w1280");

            foreach (var cast in root.Credits.Cast)
            {
                if (!string.IsNullOrEmpty(cast.ProfilePath))
                    cast.ProfilePath = GetImageUrl(cast.ProfilePath, "w185");
            }

            foreach (var similar in root.Similar.Results)
            {
                if (!string.IsNullOrEmpty(similar.PosterPath))
                    similar.PosterPath = GetImageUrl(similar.PosterPath, "w500");
                if (!string.IsNullOrEmpty(similar.BackdropPath))
                    similar.BackdropPath = GetImageUrl(similar.BackdropPath, "w1280");
            }

            return root;
        }

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week")
        {
            var url = $"{_settings.BaseUrl}/trending/{mediaType}/{timeWindow}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<TmdbResponse>(response);
        }

        public async Task<TmdbContentDetails> GetContentDetailsAsync(string mediaType, int id)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<TmdbContentDetails>(response);
        }

        public async Task<TmdbCredits> GetCreditsAsync(string mediaType, int id)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}/credits?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<TmdbCredits>(response);
        }

        public async Task<TmdbResponse> GetSimilarAsync(string mediaType, int id)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}/similar?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<TmdbResponse>(response);
        }

        public string GetImageUrl(string path, string size = "w500")
        {
            if (string.IsNullOrEmpty(path))
                return null;
            return $"{_settings.ImageBaseUrl}{size}{path}";
        }
    }
}