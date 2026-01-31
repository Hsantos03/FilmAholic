using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilmesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly FilmAholicDbContext _context;

        public FilmesController(IMovieService movieService, FilmAholicDbContext context)
        {
            _movieService = movieService;
            _context = context;
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
                catch (Exception ex)
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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);
            
            if (filme == null)
            {
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            return Ok(filme);
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
    }
}
