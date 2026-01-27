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

        /// <summary>
        /// Get all movies from database (always include popular TMDb movies if DB has few movies)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dbMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            // If database has few movies (less than 10), fetch and add popular movies from TMDb
            if (dbMovies.Count < 10)
            {
                try
                {
                    var popularMovies = await _movieService.GetPopularMoviesAsync(page: 1, count: 20);
                    if (popularMovies.Any())
                    {
                        // Save popular movies to database so they have valid IDs
                        foreach (var movie in popularMovies)
                        {
                            // Check if movie already exists (by TmdbId or title)
                            var existing = await _context.Set<Models.Filme>()
                                .FirstOrDefaultAsync(f => 
                                    (!string.IsNullOrEmpty(movie.TmdbId) && f.TmdbId == movie.TmdbId) ||
                                    f.Titulo == movie.Titulo);
                            
                            if (existing == null)
                            {
                                // Don't set Id - let database generate it
                                movie.Id = 0;
                                _context.Set<Models.Filme>().Add(movie);
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue - we'll return what we have in DB
                    // If TMDb fails and DB is empty, fallback to seed
                    if (!dbMovies.Any())
                    {
                        return Ok(FilmSeed.Filmes);
                    }
                }
            }

            // Reload all movies from database (including newly added popular ones)
            var allMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            // If still empty after trying TMDb, return seed as fallback
            if (!allMovies.Any())
            {
                return Ok(FilmSeed.Filmes);
            }

            return Ok(allMovies);
        }

        /// <summary>
        /// Get movie by ID from database or seed
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);
            
            if (filme == null)
            {
                // Fallback to seed
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            return Ok(filme);
        }

        /// <summary>
        /// Search movies using TMDb API
        /// </summary>
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
        /// Get movie details from TMDb by TMDb ID
        /// </summary>
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

        /// <summary>
        /// Get or create a movie in the database from TMDb ID
        /// </summary>
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

        /// <summary>
        /// Update movie information from external APIs
        /// </summary>
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
    }
}
