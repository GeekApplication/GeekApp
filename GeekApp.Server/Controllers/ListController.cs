using GeekApp.Server.Services;
using GeekApp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeekApp.Server.Controllers
{
    [Route("api/lists")]
    [ApiController]
    [Authorize]
    public class ListController : ControllerBase
    {
        private readonly IListService _listService;

        public ListController(IListService listService)
        {
            _listService = listService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateList([FromBody] CreateListRequest request)
        {
            var list = await _listService.CreateListAsync(request.Name);
            return Ok(list);
        }

        [HttpGet("watchlist")]
        public async Task<IActionResult> GetWatchlist()
        {
            var watchlist = await _listService.GetWatchlistAsync();
            return Ok(watchlist);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetUserLists()
        {
            var lists = await _listService.GetUserListsAsync();
            return Ok(lists);
        }

        [HttpDelete("{listId}")]
        public async Task<IActionResult> DeleteList(string listId)
        {
            await _listService.DeleteListAsync(listId);
            return Ok();
        }

        [HttpPost("watchlist/add")]
        public async Task<IActionResult> AddToWatchlist([FromBody] ListItemRequest request)
        {
            await _listService.AddToWatchlistAsync(request.MediaType, request.TmdbId);
            return Ok();
        }

        [HttpPost("watchlist/remove")]
        public async Task<IActionResult> RemoveFromWatchlist([FromBody] ListItemRequest request)
        {
            await _listService.RemoveFromWatchlistAsync(request.MediaType, request.TmdbId);
            return Ok();
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToLists([FromBody] AddToListsRequest request)
        {
            await _listService.AddToListsAsync(request.MediaType, request.TmdbId, request.ListIds);
            return Ok();
        }

        [HttpPost("remove")]
        public async Task<IActionResult> RemoveFromLists([FromBody] AddToListsRequest request)
        {
            await _listService.RemoveFromListsAsync(request.MediaType, request.TmdbId, request.ListIds);
            return Ok();
        }

        [HttpGet("items/{listId}")]
        public async Task<IActionResult> GetListItems(string listId)
        {
            var items = await _listService.GetListItemsAsync(listId);
            return Ok(items);
        }

        [HttpGet("is-in-watchlist/{mediaType}/{tmdbId}")]
        public async Task<IActionResult> IsInWatchlist([FromQuery] string mediaType, [FromQuery] int tmdbId)
        {
            var isInWatchlist = await _listService.IsInWatchlistAsync(mediaType, tmdbId);
            return Ok(isInWatchlist);
        }

        [HttpGet("title/{mediaType}/{tmdbId}")]
        public async Task<IActionResult> GetListsContainingTitle(string mediaType, int tmdbId)
        {
            var listIds = await _listService.GetListsContainingTitleAsync(mediaType, tmdbId);
            return Ok(listIds);
        }
    }

    public class CreateListRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ListItemRequest
    {
        public string MediaType { get; set; } = string.Empty;
        public int TmdbId { get; set; }
    }

    public class AddToListsRequest
    {
        public string MediaType { get; set; } = string.Empty;
        public int TmdbId { get; set; }
        public List<string> ListIds { get; set; } = new List<string>();
    }
}