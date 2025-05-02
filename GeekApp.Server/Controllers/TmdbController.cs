using GeekApp.Shared.ApiModels;
using Microsoft.AspNetCore.Mvc;
using GeekApp.Server.Services;
using System.Threading.Tasks;

namespace GeekApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TmdbController : ControllerBase
    {
        private readonly ITmdbService _tmdbService;

        public TmdbController(ITmdbService tmdbService)
        {
            _tmdbService = tmdbService;
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
    }
}