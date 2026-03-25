using FilmAholic.Server.Data;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmAholic.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly FilmAholicDbContext _context;
    private readonly ICinemaScraperService _scraperService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(FilmAholicDbContext context, ICinemaScraperService scraperService, ILogger<CacheController> logger)
    {
        _context = context;
        _scraperService = scraperService;
        _logger = logger;
    }

    [HttpPost("clear-cache")]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            // Clear existing cache
            var deleted = await _context.CinemaMovieCache.ExecuteDeleteAsync();
            
            // Scrape with new URLs
            var movies = await _scraperService.ScrapeAllAsync();
            
            _logger.LogInformation("Cache cleared and refreshed: {Count} movies", movies.Count);
            
            return Ok(new { 
                message = "Cache cleared successfully", 
                deleted = deleted,
                newMovies = movies.Count,
                nosMovies = movies.Count(m => m.Cinema == "Cinema NOS"),
                cityMovies = movies.Count(m => m.Cinema == "Cinema City")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
