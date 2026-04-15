using FilmAholic.Server.Data;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers;

    /// <summary>
    /// Controlador administrativo para a gestão da aplicação, e intervenção direta no sistema cache de cinemas.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CacheController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;
        private readonly ICinemaScraperService _scraperService;
        private readonly ILogger<CacheController> _logger;

        /// <summary>
        /// Construtor do controlador de cacheamento.
        /// Injeta contextos da DB e do Scraper e instância o provedor de logs.
        /// </summary>
        /// <param name="context">O contexto estrutural da Entity Framework.</param>
        /// <param name="scraperService">A interface de extração massiva de dados dos distribuidores cinemáticos locais.</param>
        /// <param name="logger">Responsável pelo registo sistemático das operações.</param>
        public CacheController(FilmAholicDbContext context, ICinemaScraperService scraperService, ILogger<CacheController> logger)
        {
            _context = context;
            _scraperService = scraperService;
            _logger = logger;
        }

        /// <summary>
        /// Limpa o cahe residente e volta a invocar e capturar dados de distribuição local utilizando as rotinas normais do web scraper suportado.
        /// </summary>
        /// <returns>Feedback contendo estatísticas da quantidade apagada, adicionada, e distribuições entre as duas fornecedoras de cine-teatros associadas (NOS e City).</returns>
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
