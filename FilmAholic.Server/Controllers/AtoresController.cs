using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmAholic.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AtoresController : ControllerBase
{
    private readonly IMovieService _movieService;

    public AtoresController(IMovieService movieService)
    {
        _movieService = movieService;
    }

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularActors([FromQuery] int count = 100)
    {
        var actors = await _movieService.GetPopularActorsAsync(page: 1, count: count);
        return Ok(actors);
    }
}

