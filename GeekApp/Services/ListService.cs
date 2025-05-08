using GeekApp.Shared.Lists;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace GeekApp.Services
{
    public interface IListService
    {
        Task<AddList> GetWatchlistAsync();
        Task<List<AddList>> GetUserListsAsync();
        Task<bool> CreateListAsync(string listName);
        Task<bool> DeleteListAsync(string listId);
        Task<bool> AddToWatchlistAsync(string mediaType, int tmdbId);
        Task<bool> AddToListAsync(string listId, string mediaType, int tmdbId);
        Task<bool> RemoveFromWatchlistAsync(string mediaType, int tmdbId);
        Task<bool> RemoveFromListAsync(string listId, string mediaType, int tmdbId);
        Task<bool> IsInWatchlistAsync(string mediaType, int tmdbId);
        Task<List<AddList>> GetListsContainingTitleAsync(string mediaType, int tmdbId);
        Task<List<ListItem>> GetListItemsAsync(string listId);
    }

    public class ListService : IListService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ListService> _logger;

        public ListService(IHttpClientFactory factory, ILogger<ListService> logger)
        {
            _http = factory.CreateClient("AuthorizedAPI");
            _logger = logger;
        }

        public async Task<AddList> GetWatchlistAsync()
        {
            try
            {
                _logger.LogDebug("Fetching watchlist");
                var response = await _http.GetAsync("/api/lists/watchlist");
                response.EnsureSuccessStatusCode();
                var watchlist = await response.Content.ReadFromJsonAsync<AddList>();
                _logger.LogInformation("Successfully fetched watchlist with {ItemCount} items", watchlist?.Items?.Count ?? 0);
                return watchlist;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to fetch watchlist, Status: {Status}", ex.StatusCode);
                return new AddList { Items = new List<ListItem>() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching watchlist");
                return new AddList { Items = new List<ListItem>() };
            }
        }

        public async Task<List<AddList>> GetUserListsAsync()
        {
            try
            {
                _logger.LogDebug("Fetching user lists");
                _logger.LogDebug("Sending request to {Uri}", $"{_http.BaseAddress}api/lists/all");
                var response = await _http.GetAsync("api/lists/all");
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                var lists = await response.Content.ReadFromJsonAsync<List<AddList>>();
                _logger.LogInformation("User lists retrieved successfully. Count: {Count}", lists?.Count ?? 0);
                return lists ?? new List<AddList>();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Get user lists failed. Status code: Unauthorized");
                throw new UnauthorizedAccessException("Authentication required to access user lists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get user lists failed.");
                throw;
            }
        }

        public async Task<bool> CreateListAsync(string listName)
        {
            try
            {
                _logger.LogDebug("Creating list: {ListName}", listName);
                var response = await _http.PostAsJsonAsync("api/lists/create", new { Name = listName });
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("List created successfully: {ListName}", listName);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Create list failed. Status code: Unauthorized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create list failed: {ListName}", listName);
                return false;
            }
        }

        public async Task<bool> DeleteListAsync(string listId)
        {
            try
            {
                _logger.LogDebug("Deleting list: {ListId}", listId);
                var response = await _http.DeleteAsync($"api/lists/{listId}");
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("List deleted successfully: {ListId}", listId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Delete list failed. Status code: Unauthorized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete list failed: {ListId}", listId);
                return false;
            }
        }

        public async Task<bool> AddToWatchlistAsync(string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Adding to watchlist: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                var response = await _http.PostAsJsonAsync("api/lists/watchlist/add", new { MediaType = mediaType, TmdbId = tmdbId });
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Added to watchlist successfully: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Add to watchlist failed. Status code: Unauthorized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add to watchlist failed: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                return false;
            }
        }

        public async Task<bool> AddToListAsync(string listId, string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Adding to list: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                var request = new { MediaType = mediaType, TmdbId = tmdbId, ListIds = new List<string> { listId } };
                var response = await _http.PostAsJsonAsync("api/lists/add", request);
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Added to list successfully: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Add to list failed. Status code: Unauthorized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Add to list failed: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                return false;
            }
        }

        public async Task<bool> RemoveFromWatchlistAsync(string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Removing from watchlist: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                var response = await _http.PostAsJsonAsync("api/lists/watchlist/remove", new { MediaType = mediaType, TmdbId = tmdbId });
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Removed from watchlist successfully: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Remove from watchlist failed. Status code: Unauthorized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Remove from watchlist failed: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                return false;
            }
        }

        public async Task<bool> RemoveFromListAsync(string listId, string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Removing from list: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                var request = new { MediaType = mediaType, TmdbId = tmdbId, ListIds = new List<string> { listId } };
                _logger.LogDebug("Sending request to api/lists/remove with body: {@Request}", request);
                var response = await _http.PostAsJsonAsync("api/lists/remove", request);
                _logger.LogDebug("Received response with status code: {StatusCode}", response.StatusCode);
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Removed from list successfully: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                return true;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogWarning("Unauthorized access when removing from list: {ListId}.", listId);
                throw new UnauthorizedAccessException("User is not authorized to remove from list.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove from list: {ListId}, {MediaType}, TMDB ID: {TmdbId}", listId, mediaType, tmdbId);
                return false;
            }
        }

        public async Task<bool> IsInWatchlistAsync(string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Checking watchlist status for {MediaType}, {TmdbId}", mediaType, tmdbId);
                var response = await _http.GetAsync($"/api/lists/is-in-watchlist/{mediaType}/{tmdbId}");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Watchlist check endpoint not found for {MediaType}, {TmdbId}", mediaType, tmdbId);
                    return false;
                }
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<bool>();
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Failed to check watchlist status for {MediaType}, {TmdbId}, Status: {Status}", mediaType, tmdbId, ex.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error checking watchlist status for {MediaType}, {TmdbId}", mediaType, tmdbId);
                return false;
            }
        }

        public async Task<List<AddList>> GetListsContainingTitleAsync(string mediaType, int tmdbId)
        {
            try
            {
                _logger.LogDebug("Fetching lists containing title: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                var response = await _http.GetAsync($"/api/lists/title/{mediaType}/{tmdbId}");
                response.EnsureSuccessStatusCode();
                var listIds = await response.Content.ReadFromJsonAsync<List<string>>();
                var allLists = await GetUserListsAsync();
                var matchingLists = allLists.Where(l => listIds.Contains(l.Id)).ToList();
                _logger.LogInformation("Successfully fetched {Count} lists containing title: {MediaType}, TMDB ID: {TmdbId}", matchingLists.Count, mediaType, tmdbId);
                return matchingLists;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                _logger.LogError(ex, "Failed to fetch lists containing title due to unauthorized access");
                throw new UnauthorizedAccessException("Authentication required to access lists.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch lists containing title: {MediaType}, TMDB ID: {TmdbId}", mediaType, tmdbId);
                return new List<AddList>();
            }
        }

        public async Task<List<ListItem>> GetListItemsAsync(string listId)
        {
            try
            {
                var response = await _http.GetAsync($"api/lists/items/{listId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<ListItem>>() ?? new List<ListItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch items for list {listId}");
                return new List<ListItem>();
            }
        }
    }
}
    
