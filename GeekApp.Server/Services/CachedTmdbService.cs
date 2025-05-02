using GeekApp.Shared.ApiModels;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

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

        public async Task<TmdbResponse> GetTrendingAsync(string mediaType = "all", string timeWindow = "week")
        {
            var cacheKey = $"trending_{mediaType}_{timeWindow}";

            try
            {
                return await _cache.GetOrCreateAsync(cacheKey, async entry =>
                {
                    entry.SetOptions(_cacheOptions);
                    var result = await _tmdbService.GetTrendingAsync(mediaType, timeWindow);
                    _logger.LogInformation($"Fetched fresh trending data for {mediaType}/{timeWindow}");
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching trending {mediaType} data");
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

        public string GetImageUrl(string path, string size = "w500")
        {
            // No caching for image URLs as they're already CDN-based
            return _tmdbService.GetImageUrl(path, size);
        }

        private void OnCacheEviction(object key, object value, EvictionReason reason, object state)
        {
            _logger.LogInformation($"Cache entry evicted: {key} - Reason: {reason}");
        }
    }
}