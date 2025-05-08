using GeekApp.Server.Models;
using GeekApp.Shared.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using GeekApp.Shared.Lists;

namespace GeekApp.Server.Services
{
    public interface IListService
    {
        Task<AddList> CreateListAsync(string listName);
        Task<AddList> GetWatchlistAsync();
        Task<List<AddList>> GetUserListsAsync();
        Task DeleteListAsync(string listId);
        Task AddToWatchlistAsync(string mediaType, int tmdbId);
        Task RemoveFromWatchlistAsync(string mediaType, int tmdbId);
        Task AddToListsAsync(string mediaType, int tmdbId, List<string> listIds);
        Task RemoveFromListsAsync(string mediaType, int tmdbId, List<string> listIds);
        Task<List<ListItem>> GetListItemsAsync(string listId);
        Task<bool> IsInWatchlistAsync(string mediaType, int tmdbId);
        Task<List<string>> GetListsContainingTitleAsync(string mediaType, int tmdbId);
    }

    public class ListService : IListService
    {
        private readonly IMongoCollection<AddList> _lists;
        private readonly IMongoCollection<ListItem> _listItems;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ListService> _logger;

        public ListService(MongoDBService mongoDBService, IHttpContextAccessor httpContextAccessor, ILogger<ListService> logger)
        {
            _lists = mongoDBService.Lists;
            _listItems = mongoDBService.ListItems;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private string GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
            {
                _logger.LogError("HttpContext.User is null or not authenticated.");
                throw new UnauthorizedAccessException("User is not authenticated.");
            }

            var possibleClaims = new[]
            {
                "nameid",
                "sub",
                ClaimTypes.NameIdentifier,
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            };

            string userId = null;
            foreach (var claimType in possibleClaims)
            {
                userId = user.FindFirst(claimType)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    _logger.LogDebug("Found userId {UserId} in claim {ClaimType}", userId, claimType);
                    break;
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("No valid userId found in claims: {Claims}", string.Join(", ", user.Claims.Select(c => $"{c.Type}: {c.Value}")));
                throw new UnauthorizedAccessException("User ID not found in token.");
            }

            return userId;
        }

        public async Task<AddList> CreateListAsync(string listName)
        {
            var userId = GetUserId();
            _logger.LogInformation("Creating list '{ListName}' for user {UserId}", listName, userId);
            var list = new AddList
            {
                UserId = userId,
                Name = listName,
                IsWatchlist = false,
                CreatedAt = DateTime.UtcNow,
                Items = new List<ListItem>()
            };
            await _lists.InsertOneAsync(list);
            return list;
        }

        public async Task<AddList> GetWatchlistAsync()
        {
            var userId = GetUserId();
            var watchlist = await _lists.Find(l => l.UserId == userId && l.IsWatchlist).FirstOrDefaultAsync();
            if (watchlist == null)
                throw new Exception("Watchlist not found.");
            watchlist.Items = await GetListItemsAsync(watchlist.Id);
            return watchlist;
        }

        public async Task<List<AddList>> GetUserListsAsync()
        {
            var userId = GetUserId();
            var lists = await _lists.Find(l => l.UserId == userId).ToListAsync();
            foreach (var list in lists)
            {
                list.Items = await GetListItemsAsync(list.Id);
            }
            return lists;
        }

        public async Task DeleteListAsync(string listId)
        {
            var userId = GetUserId();
            var list = await _lists.Find(l => l.Id == listId && l.UserId == userId).FirstOrDefaultAsync();
            if (list == null)
                throw new Exception("List not found or access denied.");
            if (list.IsWatchlist)
                throw new InvalidOperationException("Cannot delete the watchlist.");

            await _listItems.DeleteManyAsync(i => i.ListId == listId);
            await _lists.DeleteOneAsync(l => l.Id == listId);
        }

        public async Task AddToWatchlistAsync(string mediaType, int tmdbId)
        {
            var userId = GetUserId();
            var watchlist = await GetWatchlistAsync();
            var existingItem = await _listItems.Find(i => i.ListId == watchlist.Id && i.MediaType == mediaType && i.TmdbId == tmdbId).FirstOrDefaultAsync();
            if (existingItem == null)
            {
                var item = new ListItem
                {
                    ListId = watchlist.Id,
                    MediaType = mediaType,
                    TmdbId = tmdbId,
                    AddedAt = DateTime.UtcNow
                };
                await _listItems.InsertOneAsync(item);
                watchlist.Items.Add(item);
                await _lists.ReplaceOneAsync(l => l.Id == watchlist.Id, watchlist);
            }
        }

        public async Task RemoveFromWatchlistAsync(string mediaType, int tmdbId)
        {
            var userId = GetUserId();
            var watchlist = await GetWatchlistAsync();
            await _listItems.DeleteManyAsync(i => i.ListId == watchlist.Id && i.MediaType == mediaType && i.TmdbId == tmdbId);
            watchlist.Items.RemoveAll(i => i.MediaType == mediaType && i.TmdbId == tmdbId);
            await _lists.ReplaceOneAsync(l => l.Id == watchlist.Id, watchlist);
        }

        public async Task AddToListsAsync(string mediaType, int tmdbId, List<string> listIds)
        {
            var userId = GetUserId();
            foreach (var listId in listIds)
            {
                var list = await _lists.Find(l => l.Id == listId && l.UserId == userId).FirstOrDefaultAsync();
                if (list == null)
                    continue;

                var existingItem = await _listItems.Find(i => i.ListId == listId && i.MediaType == mediaType && i.TmdbId == tmdbId).FirstOrDefaultAsync();
                if (existingItem == null)
                {
                    var item = new ListItem
                    {
                        ListId = listId,
                        MediaType = mediaType,
                        TmdbId = tmdbId,
                        AddedAt = DateTime.UtcNow
                    };
                    await _listItems.InsertOneAsync(item);
                    list.Items.Add(item);
                    await _lists.ReplaceOneAsync(l => l.Id == listId, list);
                }
            }
        }

        public async Task RemoveFromListsAsync(string mediaType, int tmdbId, List<string> listIds)
        {
            var userId = GetUserId();
            foreach (var listId in listIds)
            {
                var list = await _lists.Find(l => l.Id == listId && l.UserId == userId).FirstOrDefaultAsync();
                if (list == null)
                    continue;

                await _listItems.DeleteManyAsync(i => i.ListId == listId && i.MediaType == mediaType && i.TmdbId == tmdbId);
                list.Items.RemoveAll(i => i.MediaType == mediaType && i.TmdbId == tmdbId);
                await _lists.ReplaceOneAsync(l => l.Id == listId, list);
            }
        }

        public async Task<List<ListItem>> GetListItemsAsync(string listId)
        {
            var userId = GetUserId();
            var list = await _lists.Find(l => l.Id == listId && l.UserId == userId).FirstOrDefaultAsync();
            if (list == null)
                throw new Exception("List not found or access denied.");
            return await _listItems.Find(i => i.ListId == listId).ToListAsync();
        }

        public async Task<bool> IsInWatchlistAsync(string mediaType, int tmdbId)
        {
            var watchlist = await GetWatchlistAsync();
            return watchlist.Items.Any(i => i.MediaType == mediaType && i.TmdbId == tmdbId);
        }

        public async Task<List<string>> GetListsContainingTitleAsync(string mediaType, int tmdbId)
        {
            var userId = GetUserId();
            var items = await _listItems.Find(i => i.MediaType == mediaType && i.TmdbId == tmdbId).ToListAsync();
            var listIds = items.Select(i => i.ListId).ToList();
            var lists = await _lists.Find(l => l.UserId == userId && listIds.Contains(l.Id)).ToListAsync();
            return lists.Select(l => l.Id).ToList();
        }
    }
}