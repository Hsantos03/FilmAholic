using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecomendacoesController : ControllerBase
{
    private readonly FilmAholicDbContext _context;
    private readonly IPreferenciasService _preferenciasService;
    private readonly IMovieService _movieService;

    public RecomendacoesController(
        FilmAholicDbContext context,
        IPreferenciasService preferenciasService,
        IMovieService movieService)
    {
        _context = context;
        _preferenciasService = preferenciasService;
        _movieService = movieService;
    }


    [HttpGet("personalizadas")]
    public async Task<ActionResult<List<RecomendacaoDto>>> GetRecomendacoesPersonalizadas(
        [FromQuery] int limit = 20,
        [FromQuery] double minRating = 6.5)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (limit < 1) limit = 1;
        if (limit > 50) limit = 50;

        var generosFavoritos = await _preferenciasService.ObterGenerosFavoritosAsync(userId);
        if (generosFavoritos == null || generosFavoritos.Count == 0)
            return Ok(new List<RecomendacaoDto>());

        var genreNames = generosFavoritos
            .Select(g => g.Nome.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var watchedFilmeIds = await _context.UserMovies
            .Where(um => um.UtilizadorId == userId && um.JaViu)
            .Select(um => um.FilmeId)
            .ToListAsync();

        var watchedSet = new HashSet<int>(watchedFilmeIds);

        var allMovies = await _context.Set<Filme>().ToListAsync();

        var candidates = allMovies
            .Where(f => !watchedSet.Contains(f.Id))
            .Where(f =>
            {
                var movieGenres = (f.Genero ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(g => g.ToLowerInvariant())
                    .ToList();
                return movieGenres.Any(mg => genreNames.Contains(mg));
            })
            .ToList();

        if (candidates.Count == 0)
            return Ok(new List<RecomendacaoDto>());

        var candidateIds = candidates.Select(f => f.Id).ToList();

        var ratingsLookup = await _context.MovieRatings
            .Where(r => candidateIds.Contains(r.FilmeId))
            .GroupBy(r => r.FilmeId)
            .Select(g => new
            {
                FilmeId = g.Key,
                Average = g.Average(r => (double)r.Score),
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.FilmeId);

        var results = candidates
            .Select(f =>
            {
                ratingsLookup.TryGetValue(f.Id, out var rating);
                var avg = rating?.Average ?? 0;
                var count = rating?.Count ?? 0;
                return new RecomendacaoDto
                {
                    Id = f.Id,
                    Titulo = f.Titulo,
                    PosterUrl = f.PosterUrl,
                    Genero = f.Genero,
                    Ano = f.Ano,
                    TmdbId = f.TmdbId,
                    Duracao = f.Duracao,
                    CommunityAverage = Math.Round(avg, 1),
                    CommunityVotes = count
                };
            })
            .OrderByDescending(r => r.CommunityVotes > 0 ? r.CommunityAverage : -1)
            .ThenByDescending(r => r.CommunityVotes)
            .Where(r => r.CommunityVotes == 0 || r.CommunityAverage >= minRating)
            .Take(limit)
            .ToList();

        return Ok(results);
    }
}

public class RecomendacaoDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string PosterUrl { get; set; } = "";
    public string Genero { get; set; } = "";
    public int? Ano { get; set; }
    public string TmdbId { get; set; } = "";
    public int Duracao { get; set; }
    public double CommunityAverage { get; set; }
    public int CommunityVotes { get; set; }
}