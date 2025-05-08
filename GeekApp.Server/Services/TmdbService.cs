using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
        Task<TmdbResponse> GetTrendingAsync(string mediaType, string timeWindow, int page = 1, int? pageSize = null);
        Task<TmdbContentDetails> GetContentDetailsAsync(string mediaType, int id);
        Task<TmdbCredits> GetCreditsAsync(string mediaType, int id);
        Task<TmdbResponse> GetSimilarAsync(string mediaType, int id);
        Task<TmdbSeason> GetSeasonDetailsAsync(int tvId, int seasonNumber);
        Task<TmdbEpisode> GetEpisodeDetailsAsync(int tvId, int seasonNumber, int episodeNumber);
        Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", string sortBy = null, int? year = null, int? minVoteCount = null, int? pageSize = null);
        Task<TmdbResponse> DiscoverAsync(string mediaType, int page = 1, string sortBy = "popularity.desc", int? minYear = null, int? maxYear = null, List<int> genres = null, int? minVoteCount = 300);
        Task<List<TmdbGenre>> GetGenresAsync(string mediaType);
        string GetImageUrl(string path, string size = "w500");
        Task<TmdbResult> GetTitleSummaryAsync(string mediaType, int id);
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

            var details = JsonConvert.DeserializeObject<TmdbContentDetails>(response);
            var credits = JsonConvert.DeserializeObject<TmdbCredits>(JsonConvert.SerializeObject(jsonData.credits));
            var similar = JsonConvert.DeserializeObject<TmdbResponse>(JsonConvert.SerializeObject(jsonData.similar));

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

            root.Details.PosterPath = GetImageUrl(root.Details.PosterPath, "w500");
            root.Details.BackdropPath = GetImageUrl(root.Details.BackdropPath, "w1280");

            foreach (var cast in root.Credits?.Cast ?? new List<TmdbCast>())
            {
                cast.ProfilePath = GetImageUrl(cast.ProfilePath, "w185");
            }

            foreach (var similarItem in root.Similar?.Results ?? new List<TmdbResult>())
            {
                similarItem.PosterPath = GetImageUrl(similarItem.PosterPath, "w500");
                similarItem.BackdropPath = GetImageUrl(similarItem.BackdropPath, "w1280");
            }

            foreach (var season in root.Details.Seasons ?? new List<TmdbSeason>())
            {
                season.PosterPath = GetImageUrl(season.PosterPath, "w300");
                foreach (var episode in season.Episodes ?? new List<TmdbEpisode>())
                {
                    episode.StillPath = GetImageUrl(episode.StillPath, "w300");
                }
            }

            foreach (var company in root.Details.ProductionCompanies ?? new List<TmdbProductionCompany>())
            {
                company.LogoPath = GetImageUrl(company.LogoPath, "w200");
            }

            return root;
        }

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", int page = 1, int? pageSize = null)
        {
            var results = new List<TmdbResult>();
            if (mediaType == "all")
            {
                var movieUrl = $"{_settings.BaseUrl}/trending/movie/{timeWindow}?api_key={_settings.ApiKey}&page={page}";
                var movieResponse = await _httpClient.GetStringAsync(movieUrl);
                var movieResult = JsonConvert.DeserializeObject<TmdbResponse>(movieResponse);
                foreach (var item in movieResult.Results)
                {
                    item.MediaType = "movie";
                    item.PosterPath = GetImageUrl(item.PosterPath, "w300");
                }
                results.AddRange(movieResult.Results);

                var tvUrl = $"{_settings.BaseUrl}/trending/tv/{timeWindow}?api_key={_settings.ApiKey}&page={page}";
                var tvResponse = await _httpClient.GetStringAsync(tvUrl);
                var tvResult = JsonConvert.DeserializeObject<TmdbResponse>(tvResponse);
                foreach (var item in tvResult.Results)
                {
                    item.MediaType = "tv";
                    item.PosterPath = GetImageUrl(item.PosterPath, "w300");
                }
                results.AddRange(tvResult.Results);

                return new TmdbResponse
                {
                    Page = page,
                    Results = results.OrderByDescending(r => r.Popularity).ToList(),
                    TotalPages = Math.Max(movieResult.TotalPages, tvResult.TotalPages),
                    TotalResults = movieResult.TotalResults + tvResult.TotalResults
                };
            }
            else
            {
                var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
                var url = $"{_settings.BaseUrl}/trending/{endpoint}/{timeWindow}?api_key={_settings.ApiKey}&page={page}";
                var response = await _httpClient.GetStringAsync(url);
                var result = JsonConvert.DeserializeObject<TmdbResponse>(response);
                foreach (var item in result.Results)
                {
                    item.MediaType = mediaType.ToLower();
                    item.PosterPath = GetImageUrl(item.PosterPath, "w300");
                }
                results.AddRange(result.Results);

                Console.WriteLine($"Trending API MediaType: {mediaType}, TimeWindow: {timeWindow}, Page: {page}");
                Console.WriteLine($"Trending Results Count: {results.Count}");

                return new TmdbResponse
                {
                    Page = page,
                    Results = results.OrderByDescending(r => r.Popularity).ToList(),
                    TotalPages = result.TotalPages,
                    TotalResults = result.TotalResults
                };
            }
        }

        public async Task<TmdbContentDetails> GetContentDetailsAsync(string mediaType, int id)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}?api_key={_settings.ApiKey}&append_to_response=videos";
            var response = await _httpClient.GetStringAsync(url);
            var details = JsonConvert.DeserializeObject<TmdbContentDetails>(response);
            details.PosterPath = GetImageUrl(details.PosterPath, "w500");
            details.BackdropPath = GetImageUrl(details.BackdropPath, "w1280");
            return details;
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
            var result = JsonConvert.DeserializeObject<TmdbResponse>(response);
            foreach (var item in result.Results)
            {
                item.PosterPath = GetImageUrl(item.PosterPath, "w500");
            }
            return result;
        }

        public async Task<TmdbSeason> GetSeasonDetailsAsync(int tvId, int seasonNumber)
        {
            var url = $"{_settings.BaseUrl}/tv/{tvId}/season/{seasonNumber}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var season = JsonConvert.DeserializeObject<TmdbSeason>(response);
            season.PosterPath = GetImageUrl(season.PosterPath, "w300");
            foreach (var episode in season.Episodes ?? new List<TmdbEpisode>())
            {
                episode.StillPath = GetImageUrl(episode.StillPath, "w300");
            }
            return season;
        }

        public async Task<TmdbEpisode> GetEpisodeDetailsAsync(int tvId, int seasonNumber, int episodeNumber)
        {
            var url = $"{_settings.BaseUrl}/tv/{tvId}/season/{seasonNumber}/episode/{episodeNumber}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var episode = JsonConvert.DeserializeObject<TmdbEpisode>(response);
            episode.StillPath = GetImageUrl(episode.StillPath, "w300");
            return episode;
        }

        public async Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", string sortBy = null, int? year = null, int? minVoteCount = null, int? pageSize = null)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "search/movie" : mediaType.ToLower() == "tv" ? "search/tv" : "search/multi";
            var url = $"{_settings.BaseUrl}/{endpoint}?api_key={_settings.ApiKey}&query={Uri.EscapeDataString(query)}&page={page}";
            if (year.HasValue)
                url += $"&year={year.Value}";
            if (!string.IsNullOrEmpty(sortBy))
                url += $"&sort_by={sortBy}";
            if (minVoteCount.HasValue)
                url += $"&vote_count.gte={minVoteCount.Value}";

            Console.WriteLine($"Search URL: {url}");

            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<TmdbSearchResult>(response);

            Console.WriteLine($"Search API Response: {response}");

            foreach (var item in result.Results ?? new List<TmdbResult>())
            {
                item.PosterPath = GetImageUrl(item.PosterPath, "w300");
                Console.WriteLine($"Search Item: {item.Title ?? item.Name}, PosterPath: {item.PosterPath}");
            }

            return result;
        }

        public async Task<TmdbResponse> DiscoverAsync(string mediaType, int page = 1, string sortBy = "popularity.desc", int? minYear = null, int? maxYear = null, List<int> genres = null, int? minVoteCount = null)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "discover/movie" : "discover/tv";
            var url = $"{_settings.BaseUrl}/{endpoint}?api_key={_settings.ApiKey}&page={page}&sort_by={sortBy}";

            if (mediaType.ToLower() == "movie")
            {
                if (minYear.HasValue)
                    url += $"&primary_release_date.gte={minYear.Value}-01-01";
                if (maxYear.HasValue)
                    url += $"&primary_release_date.lte={maxYear.Value}-12-31";
            }
            else // TV shows
            {
                if (minYear.HasValue)
                    url += $"&first_air_date.gte={minYear.Value}-01-01";
                if (maxYear.HasValue)
                    url += $"&first_air_date.lte={maxYear.Value}-12-31";
            }

            if (genres?.Any() == true)
                url += $"&with_genres={string.Join(",", genres.OrderBy(g => g))}";
            if (minVoteCount.HasValue)
                url += $"&vote_count.gte={minVoteCount.Value}";

            Console.WriteLine($"Discover URL: {url}");

            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<TmdbResponse>(response);

            foreach (var item in result.Results ?? new List<TmdbResult>())
            {
                item.MediaType = mediaType.ToLower();
                item.PosterPath = GetImageUrl(item.PosterPath, "w300");
            }

            return result;
        }

        public async Task<List<TmdbGenre>> GetGenresAsync(string mediaType)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "genre/movie/list" : "genre/tv/list";
            var url = $"{_settings.BaseUrl}/{endpoint}?api_key={_settings.ApiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<dynamic>(response);
            return JsonConvert.DeserializeObject<List<TmdbGenre>>(JsonConvert.SerializeObject(result.genres));
        }

        public string GetImageUrl(string path, string size = "w500")
        {
            if (string.IsNullOrEmpty(path))
                return "/images/errorimage.jpg";
            return $"{_settings.ImageBaseUrl}{size}{path}";
        }

        public async Task<TmdbResult> GetTitleSummaryAsync(string mediaType, int id)
        {
            var endpoint = mediaType.ToLower() == "movie" ? "movie" : "tv";
            var url = $"{_settings.BaseUrl}/{endpoint}/{id}?api_key={_settings.ApiKey}&language=en-US";
            var response = await _httpClient.GetStringAsync(url);
            var result = JsonConvert.DeserializeObject<TmdbResult>(response);

            result.MediaType = mediaType.ToLower();
            result.PosterPath = GetImageUrl(result.PosterPath, "w500");
            result.BackdropPath = GetImageUrl(result.BackdropPath, "w1280");

            return result;
        }
    }
}