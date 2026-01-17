using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            if (userId == null) return Unauthorized();

            // 1) Verificar se o filme existe no seed
            var seedFilme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == filmeId);
            if (seedFilme == null)
                return BadRequest("Filme inválido (não existe no catálogo hardcoded).");

            // 2) Garantir que o filme existe na BD (tabela Filmes)
            var dbFilme = await _context.Set<Filme>().FirstOrDefaultAsync(f => f.Id == filmeId);
            if (dbFilme == null)
            {
                dbFilme = new Filme
                {
                    Id = seedFilme.Id,
                    Titulo = seedFilme.Titulo,
                    Duracao = seedFilme.Duracao,
                    Genero = seedFilme.Genero,
                    PosterUrl = seedFilme.PosterUrl ?? "",
                    TmdbId = seedFilme.TmdbId ?? ""
                };

                _context.Set<Filme>().Add(dbFilme);
                await _context.SaveChangesAsync();
            }

            // 3) Adicionar ou trocar a lista
            var existing = await _context.UserMovies
                .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

            if (existing != null)
            {
                existing.JaViu = jaViu; // troca Quero Ver <-> Já Vi
                existing.Data = DateTime.Now;
            }
            else
            {
                _context.UserMovies.Add(new UserMovie
                {
                    UtilizadorId = userId,
                    FilmeId = filmeId,
                    JaViu = jaViu,
                    Data = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }



        // 🔹 FR05 – Remover filme
        [HttpDelete("remove/{filmeId}")]
        public async Task<IActionResult> RemoveMovie(int filmeId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var movie = await _context.UserMovies
                .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == filmeId);

            if (movie == null) return NotFound();

            _context.UserMovies.Remove(movie);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 🔹 Listar Quero Ver / Já Vi
        [HttpGet("list/{jaViu}")]
        public async Task<IActionResult> GetList(bool jaViu)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var movies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu == jaViu)
                .ToListAsync();

            return Ok(movies);
        }


        // 🔹 FR07 – Total hours
        [HttpGet("totalhours")]
        public async Task<IActionResult> GetTotalHours()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var totalMinutes = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .SumAsync(um => um.Filme.Duracao);

            return Ok(totalMinutes / 60.0);
        }

        // 🔹 FR08 – Estatísticas do utilizador
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var movies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .ToListAsync();

            return Ok(new
            {
                totalFilmes = movies.Count,
                totalHoras = movies.Sum(m => m.Filme.Duracao) / 60.0,
                generos = movies
                    .GroupBy(m => m.Filme.Genero)
                    .Select(g => new { genero = g.Key, total = g.Count() })
                    .OrderByDescending(x => x.total)
                    .ToList()
            });
        }
    }
}