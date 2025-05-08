using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GeekApp.Server.Services
{
    public class CachedTmdbService : ITmdbService
    {
        private readonly ITmdbService _tmdbService;
        private readonly IMemoryCache _cache;
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly ILogger<CachedTmdbService> _logger;

        public CachedTmdbService(
            ITmdbService tmdbService,
            IMemoryCache cache,
            ILogger<CachedTmdbService> logger)
        {
            _tmdbService = tmdbService;
            _cache = cache;
            _logger = logger;
            _cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(30))
                .RegisterPostEvictionCallback(OnCacheEviction);
        }

        public async Task<TmdbRoot> GetTitleDetailsAsync(string mediaType, int id)
        {
            var cacheKey = $"title_details_{mediaType}_{id}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetTitleDetailsAsync(mediaType, id);
                    _logger.LogInformation($"Fetched fresh title details for {mediaType} {id}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching title details for {mediaType} {id}");
                throw;
            }
        }

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week", int page = 1, int? pageSize = null)
        {
            var cacheKey = $"trending_{mediaType}_{timeWindow}_{page}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetTrendingAsync(mediaType, timeWindow, page);
                    _logger.LogInformation($"Fetched fresh trending data for {mediaType}/{timeWindow}/page{page}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching trending {mediaType} data for page {page}");
                throw;
            }
        }

        public async Task<TmdbContentDetails> GetContentDetailsAsync(string mediaType, int id)
        {
            var cacheKey = $"details_{mediaType}_{id}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetContentDetailsAsync(mediaType, id);
                    _logger.LogInformation($"Fetched fresh details for {mediaType} {id}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching details for {mediaType} {id}");
                throw;
            }
        }

        public async Task<TmdbCredits> GetCreditsAsync(string mediaType, int id)
        {
            var cacheKey = $"credits_{mediaType}_{id}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetCreditsAsync(mediaType, id);
                    _logger.LogInformation($"Fetched fresh credits for {mediaType} {id}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching credits for {mediaType} {id}");
                throw;
            }
        }

        public async Task<TmdbResponse> GetSimilarAsync(string mediaType, int id)
        {
            var cacheKey = $"similar_{mediaType}_{id}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetSimilarAsync(mediaType, id);
                    _logger.LogInformation($"Fetched fresh similar content for {mediaType} {id}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching similar content for {mediaType} {id}");
                throw;
            }
        }

        public async Task<TmdbSeason> GetSeasonDetailsAsync(int tvId, int seasonNumber)
        {
            var cacheKey = $"season_{tvId}_{seasonNumber}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetSeasonDetailsAsync(tvId, seasonNumber);
                    _logger.LogInformation($"Fetched fresh season details for TV {tvId} season {seasonNumber}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching season details for TV {tvId} season {seasonNumber}");
                throw;
            }
        }

        public async Task<TmdbEpisode> GetEpisodeDetailsAsync(int tvId, int seasonNumber, int episodeNumber)
        {
            var cacheKey = $"episode_{tvId}_{seasonNumber}_{episodeNumber}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetEpisodeDetailsAsync(tvId, seasonNumber, episodeNumber);
                    _logger.LogInformation($"Fetched fresh episode details for TV {tvId} season {seasonNumber} episode {episodeNumber}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching episode details for TV {tvId} season {seasonNumber} episode {episodeNumber}");
                throw;
            }
        }

        public async Task<TmdbSearchResult> SearchAsync(string query, int page = 1, string mediaType = "multi", string sortBy = null, int? year = null, int? minVoteCount = null, int? pageSize = null)
        {
            var cacheKey = $"search_{query}_{page}_{mediaType}_{sortBy}_{year}_{minVoteCount}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.SearchAsync(query, page, mediaType, sortBy, year, minVoteCount);
                    _logger.LogInformation($"Fetched fresh search results for query: {query}, page: {page}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching search results for query: {query}, page: {page}");
                throw;
            }
        }

        public async Task<TmdbResponse> DiscoverAsync(string mediaType, int page = 1, string sortBy = "popularity.desc", int? minYear = null, int? maxYear = null, List<int> genres = null, int? minVoteCount = null)
        {
            var sortedGenres = genres?.OrderBy(g => g).ToList() ?? new List<int>();
            var cacheKey = $"discover_v2_{mediaType}_{page}_{sortBy}_{minYear}_{maxYear}_{string.Join(",", sortedGenres)}_{minVoteCount}";

            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.DiscoverAsync(mediaType, page, sortBy, minYear, maxYear, genres, minVoteCount);
                    _logger.LogInformation($"Fetched fresh discover results for {mediaType}, page: {page}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching discover results for {mediaType}, page: {page}");
                throw;
            }
        }

        public async Task<List<TmdbGenre>> GetGenresAsync(string mediaType)
        {
            var cacheKey = $"genres_{mediaType}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetGenresAsync(mediaType);
                    _logger.LogInformation($"Fetched fresh genres for {mediaType}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching genres for {mediaType}");
                throw;
            }
        }

        public async Task<TmdbResult> GetTitleSummaryAsync(string mediaType, int id)
        {
            var cacheKey = $"summary_{mediaType}_{id}";
            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetTitleSummaryAsync(mediaType, id);
                    _logger.LogInformation($"Fetched fresh title summary for {mediaType} {id}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching title summary for {mediaType} {id}");
                throw;
            }
        }

        public string GetImageUrl(string path, string size = "w500")
        {
            return _tmdbService.GetImageUrl(path, size);
        }

        private void OnCacheEviction(object key, object value, EvictionReason reason, object state)
        {
            _logger.LogInformation($"Cache entry evicted: {key} - Reason: {reason}");
        }
    }
}