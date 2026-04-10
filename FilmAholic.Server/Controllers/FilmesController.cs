using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly FilmAholicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public FilmesController(IMovieService movieService, FilmAholicDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _movieService = movieService;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dbMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            if (dbMovies.Count < 10)
            {
                try
                {
                    var popularMovies = await _movieService.GetPopularMoviesAsync(page: 1, count: 20);
                    if (popularMovies.Any())
                    {
                        foreach (var movie in popularMovies)
                        {
                            var existing = await _context.Set<Models.Filme>()
                                .FirstOrDefaultAsync(f => 
                                    (!string.IsNullOrEmpty(movie.TmdbId) && f.TmdbId == movie.TmdbId) ||
                                    f.Titulo == movie.Titulo);
                            
                            if (existing == null)
                            {
                                movie.Id = 0;
                                _context.Set<Models.Filme>().Add(movie);
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    if (!dbMovies.Any())
                    {
                        return Ok(FilmSeed.Filmes);
                    }
                }
            }

            var allMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            if (!allMovies.Any())
            {
                return Ok(FilmSeed.Filmes);
            }

            return Ok(allMovies);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string query, [FromQuery] int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required.");
            }

            try
            {
                var result = await _movieService.SearchMoviesAsync(query, page);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while searching movies.", details = ex.Message });
            }
        }

        /// <summary>
        /// TMDB: lista de próximos lançamentos (paginação do TMDB).
        /// </summary>
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming([FromQuery] int page = 1, [FromQuery] int count = 20)
        {
            if (page < 1) page = 1;
            if (count < 1) count = 20;
            count = Math.Min(count, 40);

            try
            {
                var todayUtc = DateTime.UtcNow.Date;

                // Uma página TMDB tem ~20 títulos; muitos podem já ter estreado — percorremos páginas até encher `count`.
                var list = await _movieService.GetUpcomingMoviesAccumulatedAsync(page, count, todayUtc, maxPagesToScan: 12);

                return Ok(list ?? new List<Models.Filme>());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter upcoming do TMDB.", details = ex.Message });
            }
        }

        /// <summary>
        /// Filmes “clássicos” via TMDB: <c>discover</c> (pré-2000 por defeito, nota + mín. votos) ou <c>top_rated</c>.
        /// </summary>
        /// <param name="fonte"><c>discover</c> (default) ou <c>top_rated</c></param>
        /// <param name="ateData">Só para discover: data limite de estreia (yyyy-MM-dd). Default 1999-12-31.</param>
        /// <param name="minVotos">Só para discover: vote_count.gte no TMDB (default 500).</param>
        [HttpGet("classicos")]
        public async Task<IActionResult> GetClassicos(
            [FromQuery] string fonte = "discover",
            [FromQuery] int page = 1,
            [FromQuery] int count = 20,
            [FromQuery] string? ateData = null,
            [FromQuery] int minVotos = 500)
        {
            if (page < 1) page = 1;
            if (count < 1) count = 20;
            count = Math.Min(count, 40);

            try
            {
                var key = fonte.Trim().ToLowerInvariant().Replace("-", "_");
                List<Models.Filme> list = key switch
                {
                    "top_rated" or "toprated" => await _movieService.GetTopRatedMoviesAsync(page, count),
                    _ => await _movieService.GetClassicDiscoverMoviesAsync(page, count, ateData, minVotos)
                };

                return Ok(list);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter filmes clássicos.", details = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Procura na BD local pelo ID
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            // Procura na BD pelo TmdbId
            if (filme == null)
                filme = await _context.Set<Models.Filme>()
                    .FirstOrDefaultAsync(f => f.TmdbId == id.ToString());

            // Procura no seed
            if (filme == null)
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);

            // Se ainda n�o encontrou, vai buscar ao TMDB
            if (filme == null)
            {
                try
                {
                    filme = await _movieService.GetOrCreateMovieFromTmdbAsync(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao buscar filme do TMDB: {ex.Message}");
                    return NotFound();
                }
            }

            if (filme == null) return NotFound();

            return Ok(filme);
        }

        [HttpGet("tmdb/{tmdbId}")]
        public async Task<IActionResult> GetMovieFromTmdb(int tmdbId)
        {
            try
            {
                var movie = await _movieService.GetMovieInfoAsync(tmdbId);
                if (movie == null)
                {
                    return NotFound(new { error = $"Movie with TMDb ID {tmdbId} not found." });
                }

                return Ok(movie);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching movie details.", details = ex.Message });
            }
        }

        [HttpPost("tmdb/{tmdbId}")]
        public async Task<IActionResult> AddMovieFromTmdb(int tmdbId)
        {
            try
            {
                var movie = await _movieService.GetOrCreateMovieFromTmdbAsync(tmdbId);
                return Ok(movie);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while adding movie.", details = ex.Message });
            }
        }

        
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateMovie(int id)
        {
            try
            {
                var movie = await _movieService.UpdateMovieFromApisAsync(id);
                if (movie == null)
                {
                    return NotFound(new { error = $"Movie with ID {id} not found." });
                }

                return Ok(movie);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating movie.", details = ex.Message });
            }
        }

        [HttpGet("{id}/ratings")]
        public async Task<IActionResult> GetRatings(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            if (filme == null)
            {
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            var ratings = await _movieService.GetRatingsAsync(filme.TmdbId, filme.Titulo);

            return Ok(ratings);
        }

        [HttpGet("{id}/recomendacoes")]
        public async Task<IActionResult> GetRecommendations(int id, [FromQuery] int count = 10)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            if (filme == null)
            {
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            if (string.IsNullOrEmpty(filme.TmdbId) || !int.TryParse(filme.TmdbId, out var tmdbId))
            {
                try
                {
                    var searchResult = await _movieService.SearchMoviesAsync(filme.Titulo, 1);
                    var match = searchResult?.Results?.FirstOrDefault();
                    if (match != null)
                    {
                        tmdbId = match.Id;
                    }
                    else
                    {
                        return Ok(new List<Models.Filme>()); 
                    }
                }
                catch
                {
                    return Ok(new List<Models.Filme>()); 
                }
            }

            if (count <= 0) count = 10;
            if (count > 20) count = 20;

            var recommendations = await _movieService.GetRecommendationsAsync(tmdbId, count);
            return Ok(recommendations);
        }

        [HttpGet("{id}/cast")]
        public async Task<IActionResult> GetCast(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);
            if (filme == null)
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);

            int tmdbId;
            if (filme != null && !string.IsNullOrEmpty(filme.TmdbId) && int.TryParse(filme.TmdbId, out var parsed))
            {
                tmdbId = parsed;
            }
            else
            {
                tmdbId = id;
            }

            var cast = await _movieService.GetCastAsync(tmdbId);
            return Ok(cast);
        }

        [HttpGet("{id:int}/trailer")]
        public async Task<IActionResult> GetTrailer(int id)
        {
            var apiKey = _configuration["ExternalApis:TmdbApiKey"];
            var _httpClient = _httpClientFactory.CreateClient();

            string? trailerKey = null;

            foreach (var lang in new[] { "pt-PT", "en-US" })
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.themoviedb.org/3/movie/{id}/videos?api_key={apiKey}&language={lang}");

                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var results = data.GetProperty("results");

                foreach (var v in results.EnumerateArray())
                {
                    var type = v.TryGetProperty("type", out var t) ? t.GetString() : "";
                    var site = v.TryGetProperty("site", out var s) ? s.GetString() : "";
                    if (type == "Trailer" && site == "YouTube")
                    {
                        trailerKey = v.GetProperty("key").GetString();
                        break;
                    }
                }

                if (trailerKey != null) break;
            }

            if (trailerKey == null) return NotFound();

            return Ok(new { url = $"https://www.youtube.com/watch?v={trailerKey}" });
        }

        /// <summary>
        /// Filmes mais populares do TMDB com mínimo de 500 classificações (vote_count).
        /// Busca da API TMDB os filmes populares e filtra apenas os que têm 500+ votos.
        /// </summary>
        [HttpGet("populares-comunidade")]
        public async Task<IActionResult> GetPopularCommunityMovies([FromQuery] int count = 10, [FromQuery] int minRatings = 500)
        {
            if (count < 1) count = 10;
            if (count > 40) count = 40;
            if (minRatings < 1) minRatings = 500;

            try
            {
                // Buscar filmes populares do TMDB com mínimo de votos
                var movies = await _movieService.GetPopularMoviesWithMinVotesAsync(count, minRatings, maxPages: 10);
                return Ok(movies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter filmes populares do TMDB.", details = ex.Message });
            }
        }
    }
}
