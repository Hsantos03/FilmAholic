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
    private const int FixedCount = 5;

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
        [FromQuery] double minRating = 5)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var generosFavoritos = await _preferenciasService.ObterGenerosFavoritosAsync(userId);
        if (generosFavoritos == null || generosFavoritos.Count == 0)
            return Ok(new List<RecomendacaoDto>());

        var genreNames = generosFavoritos
            .Select(g => g.Nome.Trim().ToLowerInvariant())
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        var watchedIds = new HashSet<int>(
            await _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync());

        var feedback = await _context.RecomendacaoFeedbacks
            .Where(f => f.UtilizadorId == userId)
            .Select(f => new { f.FilmeId, f.Relevante })
            .ToListAsync();

        var dismissedIds = new HashSet<int>(feedback.Where(f => !f.Relevante).Select(f => f.FilmeId));
        var likedIds = new HashSet<int>(feedback.Where(f => f.Relevante).Select(f => f.FilmeId));

        var boostedGenres = new HashSet<string>(genreNames, StringComparer.OrdinalIgnoreCase);
        if (likedIds.Count > 0)
        {
            var likedMovies = await _context.Set<Filme>()
                .Where(f => likedIds.Contains(f.Id))
                .Select(f => f.Genero)
                .ToListAsync();

            foreach (var g in likedMovies)
            {
                foreach (var part in (g ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    boostedGenres.Add(part.ToLowerInvariant());
            }
        }

        var allMovies = await _context.Set<Filme>().ToListAsync();

        var candidates = allMovies
            .Where(f => !watchedIds.Contains(f.Id) && !dismissedIds.Contains(f.Id))
            .Where(f =>
            {
                var movieGenres = (f.Genero ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(g => g.ToLowerInvariant())
                    .ToList();
                return movieGenres.Any(mg => boostedGenres.Contains(mg));
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

                var movieGenres = (f.Genero ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(g => g.ToLowerInvariant())
                    .ToList();
                var likedGenreBonus = movieGenres.Any(mg => boostedGenres.Contains(mg) && !genreNames.Contains(mg)) ? 2.0 : 0;

                var sortScore = (count > 0 ? avg : 0) + likedGenreBonus;

                return new
                {
                    Dto = new RecomendacaoDto
                    {
                        Id = f.Id,
                        Titulo = f.Titulo,
                        PosterUrl = f.PosterUrl,
                        Genero = f.Genero ?? string.Empty,
                        Ano = f.Ano,
                        TmdbId = f.TmdbId,
                        Duracao = f.Duracao,
                        CommunityAverage = Math.Round(avg, 1),
                        CommunityVotes = count
                    },
                    SortScore = sortScore,
                    Votes = count
                };
            })
            .Where(x => x.Votes == 0 || x.Dto.CommunityAverage >= minRating)
            .OrderByDescending(x => x.SortScore)
            .ThenByDescending(x => x.Votes)
            .Take(FixedCount)
            .Select(x => x.Dto)
            .ToList();

        return Ok(results);
    }

    [HttpPost("feedback")]
    public async Task<IActionResult> PostFeedback([FromBody] RecomendacaoFeedbackRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (request.FilmeId <= 0)
            return BadRequest("FilmeId inválido.");

        var existing = await _context.RecomendacaoFeedbacks
            .FirstOrDefaultAsync(f => f.UtilizadorId == userId && f.FilmeId == request.FilmeId);

        if (existing != null)
        {
            existing.Relevante = request.Relevante;
            existing.CriadoEm = DateTime.UtcNow;
        }
        else
        {
            _context.RecomendacaoFeedbacks.Add(new RecomendacaoFeedback
            {
                UtilizadorId = userId,
                FilmeId = request.FilmeId,
                Relevante = request.Relevante
            });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class RecomendacaoFeedbackRequest
{
    public int FilmeId { get; set; }
    /// <summary>true = relevant (👍), false = irrelevant (👎)</summary>
    public bool Relevante { get; set; }
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