using FilmAholic.Server.DTOs;
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

    /// <summary>Pesquisa atores por nome.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<ActorSearchResultDto>>> SearchActors([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Ok(new List<ActorSearchResultDto>());

        var actors = await _movieService.SearchActorsAsync(query!);
        return Ok(actors);
    }

    /// <summary>Lista todos os filmes em que o ator participa.</summary>
    [HttpGet("{personId:int}")]
    public async Task<ActionResult<ActorDetailsDto>> GetActorDetails([FromRoute] int personId)
    {
        var details = await _movieService.GetActorDetailsAsync(personId);
        if (details == null) return NotFound();
        return Ok(details);
    }

    /// <summary>Lista todos os filmes em que o ator participa.</summary>
    [HttpGet("{personId:int}/movies")]
    public async Task<ActionResult<List<ActorMovieDto>>> GetMoviesByActor([FromRoute] int personId)
    {
        var movies = await _movieService.GetMoviesByActorAsync(personId);
        return Ok(movies);
    }
}

