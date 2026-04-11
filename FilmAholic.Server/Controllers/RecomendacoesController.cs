using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers;

/// <summary>
/// Controlador dedicado à geração de recomendações personalizadas e ao processamento de feedback dos utilizadores face às sugestões apresentadas.
/// </summary>
[ApiController]
    [Route("api/[controller]")]
    public class RecomendacoesController : ControllerBase
    {
    private const int FixedCount = 5;

    private readonly FilmAholicDbContext _context;
    private readonly IPreferenciasService _preferenciasService;
    private readonly IMovieService _movieService;

    /// <summary>
    /// Construtor que estabelece conexões principais para injeção de dependências do módulo de recomendações.
    /// </summary>
    /// <param name="context">Contexto base de dados centralizada.</param>
    /// <param name="preferenciasService">Mecanismo de acesso direto a géneros pré-estabelecidos pelo utilizador.</param>
    /// <param name="movieService">A ponte para comunicação com a API de filmes ou fontes secundárias.</param>
    public RecomendacoesController(
            FilmAholicDbContext context,
            IPreferenciasService preferenciasService,
            IMovieService movieService)
        {
            _context = context;
            _preferenciasService = preferenciasService;
            _movieService = movieService;
        }

    /// <summary>
    /// Rastreia as escolhas passadas do cliente autenticado e sugere longas metragens apelativas não visualizadas.
    /// Utiliza exclusão de clássicos e ponderamento em cima do Rating de Estrelas e gostos (Swipes).
    /// </summary>
    /// <param name="minRating">A classificação mínima permissível na base da comunidade/globos para o filme surgir no algoritmo (Padrão: 5.0).</param>
    /// <returns>Traz lista otimizada com os top cartazes compatíveis e limitados a processar pelo motor interno de UI.</returns>
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

        // Qualquer filme já avaliado (gosto ou não) deixa de aparecer — permite pedir novos lotes sem repetir cartões.
        var feedbackFilmeIds = new HashSet<int>(feedback.Select(f => f.FilmeId));

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
            .Where(f => !watchedIds.Contains(f.Id) && !feedbackFilmeIds.Contains(f.Id))
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

        /// <summary>
        /// Processa fisicamente o sentimento ou reação perante as sugestões (Like / Pass), alimentando futuramente o algoritmo.
        /// </summary>
        /// <param name="request">Vínculo JSON detendo os identificadores entre os quais a preferência Relevante/Irrelevante face ao filme e utilizador.</param>
        /// <returns>Estado HTTP 200 de término caso os dados se integrem eficazmente no rastreio da Base de Dados.</returns>
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

/// <summary>
/// Requisição de feedback para uma recomendação de filme.
/// </summary>
public class RecomendacaoFeedbackRequest
{
    /// <summary>
    /// Identificador do filme alvo do feedback.
    /// </summary>
    public int FilmeId { get; set; }

    /// <summary>
    /// Indica se o feedback é relevante (True) ou irrelevante (False) para o filme.
    /// </summary>
    public bool Relevante { get; set; }
}

/// <summary>
/// DTO (Data Transfer Object) para encapsular informações de uma recomendação de filme.
/// </summary>
public class RecomendacaoDto
{
    /// <summary>
    /// Identificador primário da recomendação.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Título oficial.
    /// </summary>
    public string Titulo { get; set; } = "";

    /// <summary>
    /// Caminho da ligação absoluta ou parcial contendo o póster (arte de capa promocional).
    /// </summary>
    public string PosterUrl { get; set; } = "";

    /// <summary>
    /// Lista de géneros separada por vírgulas contendo.
    /// </summary>
    public string Genero { get; set; } = "";

    /// <summary>
    /// Ano que pauta a data em que marcou estreia oficial (se aplicável).
    /// </summary>
    public int? Ano { get; set; }

    /// <summary>
    /// Identificador remoto cruzado com a plataforma Open Source TMDb.
    /// </summary>
    public string TmdbId { get; set; } = "";

    /// <summary>
    /// Duração integral em Minutos.
    /// </summary>
    public int Duracao { get; set; }

    /// <summary>
    /// Somatório ponderado originado pelos utilizadores registados na plataforma.
    /// </summary>
    public double CommunityAverage { get; set; }

    /// <summary>
    /// Totais numéricos de quantas avaliações se enquadram dentro do resultado do CommunityAverage.
    /// </summary>
    public int CommunityVotes { get; set; }
}