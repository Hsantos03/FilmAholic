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
    public async Task<IActionResult> GetPopularActors([FromQuery] int page = 1, [FromQuery] int count = 10)
    {
        if (count <= 0) count = 10;
        if (count > 50) count = 50;

        var actors = await _movieService.GetPopularActorsAsync(page, count);
        return Ok(actors);
    }
}

