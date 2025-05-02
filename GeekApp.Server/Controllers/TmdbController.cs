using GeekApp.Shared.ApiModels;
using Microsoft.AspNetCore.Mvc;
using GeekApp.Server.Services;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GeekApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TmdbController : ControllerBase
    {
        private readonly ITmdbService _tmdbService;
        private readonly ILogger<TmdbController> _logger;

        public TmdbController(ITmdbService tmdbService, ILogger<TmdbController> logger)
        {
            _tmdbService = tmdbService;
            _logger = logger;
        }

        [HttpGet("title/{mediaType}/{id}")]
        public async Task<ActionResult<TmdbRoot>> GetTitleDetails(string mediaType, int id)
        {
            if (mediaType.ToLower() != "movie" && mediaType.ToLower() != "tv")
                return BadRequest("Invalid media type. Use 'movie' or 'tv'.");

            try
            {
                var result = await _tmdbService.GetTitleDetailsAsync(mediaType, id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch title details", details = ex.Message });
            }
        }

        [HttpGet("tv/{tvId}/season/{seasonNumber}/episode/{episodeNumber}")]
        public async Task<ActionResult<TmdbEpisode>> GetEpisodeDetails(int tvId, int seasonNumber, int episodeNumber)
        {
            try
            {
                var result = await _tmdbService.GetEpisodeDetailsAsync(tvId, seasonNumber, episodeNumber);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch episode details", details = ex.Message });
            }
        }

        [HttpGet("search")]
            public async Task<ActionResult<TmdbSearchResult>> Search(
                [FromQuery] string query,
                [FromQuery] int page = 1,
                [FromQuery] string mediaType = "multi",
                [FromQuery] string sortBy = null,
                [FromQuery] int? year = null,
                [FromQuery] int? pageSize = 20)
        {
            // Add cache control headers
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("X-Version", Guid.NewGuid().ToString("N")[..8]);

            try
            {
                var result = await _tmdbService.SearchAsync(query, page, mediaType, sortBy, year, pageSize);

                if (mediaType == "multi")
                {
                    result.Results = result.Results.Where(r => r.MediaType != "person").ToList();
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search API");
                return StatusCode(500, new { error = "Failed to fetch search results", details = ex.Message });
            }
        }

        [HttpGet("trending")]
        public async Task<ActionResult<TmdbResponse>> GetTrending(
            [FromQuery] string mediaType = "all",
            [FromQuery] string timeWindow = "week",
            [FromQuery] int page = 1,
            [FromQuery] int? pageSize = null)
        {
            try
            {
                var result = await _tmdbService.GetTrendingAsync(mediaType, timeWindow, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Trending API");
                return StatusCode(500, new { error = "Failed to fetch trending content", details = ex.Message });
            }
        }

        [HttpGet("discover")]
        public async Task<ActionResult<TmdbResponse>> Discover(
            [FromQuery] string mediaType = "movie",
            [FromQuery] int page = 1,
            [FromQuery] string sortBy = "popularity.desc",
            [FromQuery] int? minYear = null,
            [FromQuery] int? maxYear = null,
            [FromQuery] string genres = null,
            [FromQuery] int? minVoteCount = 300)
        {
            // Add cache control headers
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("X-Version", Guid.NewGuid().ToString("N")[..8]);

            if (mediaType.ToLower() != "movie" && mediaType.ToLower() != "tv")
                return BadRequest("Invalid media type. Use 'movie' or 'tv'.");

            try
            {
                List<int> genreList = null;
                if (!string.IsNullOrEmpty(genres))
                {
                    genreList = genres.Split(',').Select(int.Parse).ToList();
                }

                _logger.LogInformation($"Discover API called: {mediaType}, Page {page}, " +
                    $"Sort: {sortBy}, Years: {minYear}-{maxYear}, " +
                    $"Genres: {genres}, MinVotes: {minVoteCount}");

                var result = await _tmdbService.DiscoverAsync(
                    mediaType,
                    page,
                    sortBy,
                    minYear,
                    maxYear,
                    genreList,
                    minVoteCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Discover API");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = "Failed to fetch discover results",
                    details = ex.Message
                });
            }
        }

        [HttpGet("genres")]
        public async Task<ActionResult<List<TmdbGenre>>> GetGenres([FromQuery] string mediaType = "movie")
        {
            if (mediaType.ToLower() != "movie" && mediaType.ToLower() != "tv")
                return BadRequest("Invalid media type. Use 'movie' or 'tv'.");

            try
            {
                var result = await _tmdbService.GetGenresAsync(mediaType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch genres", details = ex.Message });
            }
        }
    }
}