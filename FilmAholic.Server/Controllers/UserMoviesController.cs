using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using System.Security.Claims;


namespace FilmAholic.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/usermovies")]
    public class UserMoviesController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public UserMoviesController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // 🔹 FR05 – Adicionar filme (Quero Ver / Já Vi)
        [HttpPost("add")]
        public async Task<IActionResult> AddMovie(int filmeId, bool jaViu)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (await _context.UserMovies.AnyAsync(um =>
                um.UtilizadorId == userId && um.FilmeId == filmeId))
            {
                return BadRequest("Filme já existe na lista.");
            }

            var userMovie = new UserMovie
            {
                UtilizadorId = userId,
                FilmeId = filmeId,
                JaViu = jaViu
            };

            _context.UserMovies.Add(userMovie);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 🔹 FR05 – Remover filme
        [HttpDelete("remove/{filmeId}")]
        public async Task<IActionResult> RemoveMovie(int filmeId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var movie = await _context.UserMovies
                .FirstOrDefaultAsync(um =>
                    um.UtilizadorId == userId && um.FilmeId == filmeId);

            if (movie == null)
                return NotFound();

            _context.UserMovies.Remove(movie);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 🔹 Listar Quero Ver / Já Vi
        [HttpGet("list/{jaViu}")]
        public async Task<IActionResult> GetList(bool jaViu)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var movies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu == jaViu)
                .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("totalhours")]
        [Authorize]
        public IActionResult GetTotalHours()
        {
            // 1. Pegar o ID do utilizador autenticado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // 2. Puxar todos os filmes que estão na lista "Já Vi"
            var totalMinutes = _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Sum(um => um.Filme.Duracao);

            // 3. Converter para horas (double)
            double totalHours = totalMinutes / 60.0;

            // 4. Retornar ao frontend
            return Ok(totalHours);
        }

    }
}