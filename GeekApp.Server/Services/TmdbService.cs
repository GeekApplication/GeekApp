using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        Task<TmdbSeason> GetSeasonDetailsAsync(int tvId, int seasonNumber);
        Task<TmdbEpisode> GetEpisodeDetailsAsync(int tvId, int seasonNumber, int episodeNumber);
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
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}?api_key={_settings.ApiKey}&append_to_response=credits,similar,videos";
            var response = await _httpClient.GetStringAsync(url);
            var jsonData = JsonConvert.DeserializeObject<dynamic>(response);

            // Map main details
            var details = JsonConvert.DeserializeObject<TmdbContentDetails>(response);

            // Map appended data
            var credits = JsonConvert.DeserializeObject<TmdbCredits>(JsonConvert.SerializeObject(jsonData.credits));
            var similar = JsonConvert.DeserializeObject<TmdbResponse>(JsonConvert.SerializeObject(jsonData.similar));

            // Fetch episode details for TV shows
            if (mediaType.ToLower() == "tv" && details.Seasons != null)
            {
                foreach (var season in details.Seasons)
                {
                    var seasonDetails = await GetSeasonDetailsAsync(id, season.SeasonNumber);
                    season.Episodes = seasonDetails.Episodes;
                    season.PosterPath = seasonDetails.PosterPath;
                }
            }

            var root = new TmdbRoot
            {
                Details = details,
                Credits = credits,
                Similar = similar
            };

            // Append image base URLs
            if (!string.IsNullOrEmpty(root.Details.PosterPath))
                root.Details.PosterPath = GetImageUrl(root.Details.PosterPath, "w500");
            if (!string.IsNullOrEmpty(root.Details.BackdropPath))
                root.Details.BackdropPath = GetImageUrl(root.Details.BackdropPath, "w1280");

            foreach (var cast in root.Credits?.Cast ?? new List<TmdbCast>())
            {
                if (!string.IsNullOrEmpty(cast.ProfilePath))
                    cast.ProfilePath = GetImageUrl(cast.ProfilePath, "w185");
            }

            foreach (var similarItem in root.Similar?.Results ?? new List<TmdbResult>())
            {
                if (!string.IsNullOrEmpty(similarItem.PosterPath))
                    similarItem.PosterPath = GetImageUrl(similarItem.PosterPath, "w500");
                if (!string.IsNullOrEmpty(similarItem.BackdropPath))
                    similarItem.BackdropPath = GetImageUrl(similarItem.BackdropPath, "w1280");
            }

            foreach (var season in root.Details.Seasons ?? new List<TmdbSeason>())
            {
                if (!string.IsNullOrEmpty(season.PosterPath))
                    season.PosterPath = GetImageUrl(season.PosterPath, "w300");
                foreach (var episode in season.Episodes ?? new List<TmdbEpisode>())
                {
                    if (!string.IsNullOrEmpty(episode.StillPath))
                        episode.StillPath = GetImageUrl(episode.StillPath, "w300");
                }
            }

            foreach (var company in root.Details.ProductionCompanies ?? new List<TmdbProductionCompany>())
            {
                if (!string.IsNullOrEmpty(company.LogoPath))
                    company.LogoPath = GetImageUrl(company.LogoPath, "w200");
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
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}?api_key={_settings.ApiKey}&append_to_response=videos";
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

        public async Task<TmdbSeason> GetSeasonDetailsAsync(int tvId, int seasonNumber)
        {
            var url = $"{_settings.BaseUrl}/tv/{tvId}/season/{seasonNumber}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var season = JsonConvert.DeserializeObject<TmdbSeason>(response);
            if (!string.IsNullOrEmpty(season.PosterPath))
                season.PosterPath = GetImageUrl(season.PosterPath, "w300");
            foreach (var episode in season.Episodes ?? new List<TmdbEpisode>())
            {
                if (!string.IsNullOrEmpty(episode.StillPath))
                    episode.StillPath = GetImageUrl(episode.StillPath, "w300");
            }
            return season;
        }

        public async Task<TmdbEpisode> GetEpisodeDetailsAsync(int tvId, int seasonNumber, int episodeNumber)
        {
            var url = $"{_settings.BaseUrl}/tv/{tvId}/season/{seasonNumber}/episode/{episodeNumber}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var episode = JsonConvert.DeserializeObject<TmdbEpisode>(response);
            if (!string.IsNullOrEmpty(episode.StillPath))
                episode.StillPath = GetImageUrl(episode.StillPath, "w300");
            return episode;
        }

        public string GetImageUrl(string path, string size = "w500")
        {
            if (string.IsNullOrEmpty(path))
                return "https://via.placeholder.com/300x450?text=No+Image";
            return $"{_settings.ImageBaseUrl}{size}{path}";
        }
    }
}