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
            // Procurar primeiro por TmdbId (se existir) ou Titulo para evitar criar duplicados
            Filme? dbFilme = null;
            if (!string.IsNullOrEmpty(seedFilme.TmdbId))
            {
                // Se tem TmdbId, procurar por ele
                dbFilme = await _context.Set<Filme>()
                    .FirstOrDefaultAsync(f => f.TmdbId == seedFilme.TmdbId);
            }
            
            // Se não encontrou por TmdbId ou não tem TmdbId, procurar por título
            if (dbFilme == null)
            {
                dbFilme = await _context.Set<Filme>()
                    .FirstOrDefaultAsync(f => f.Titulo == seedFilme.Titulo);
            }
            
            if (dbFilme == null)
            {
                dbFilme = new Filme
                {
                    // Não definir Id - será gerado automaticamente pelo DB
                    Titulo = seedFilme.Titulo,
                    Duracao = seedFilme.Duracao,
                    Genero = seedFilme.Genero,
                    PosterUrl = seedFilme.PosterUrl ?? "",
                    TmdbId = seedFilme.TmdbId ?? ""
                };

                _context.Set<Filme>().Add(dbFilme);
                await _context.SaveChangesAsync();
            }

            // 3) Adicionar ou trocar a lista usando o Id real do filme na BD
            var actualFilmeId = dbFilme.Id;
            var existing = await _context.UserMovies
                .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == actualFilmeId);

            bool shouldProcessDesafio = false;
            if (existing != null)
            {
                // detect transition from not-watched to watched
                var previouslyWatched = existing.JaViu;
                if (!previouslyWatched && jaViu)
                {
                    shouldProcessDesafio = true;
                }

                existing.JaViu = jaViu; // troca Quero Ver <-> Já Vi
                existing.Data = DateTime.Now;
            }
            else
            {
                _context.UserMovies.Add(new UserMovie
                {
                    UtilizadorId = userId,
                    FilmeId = actualFilmeId, // Usar o Id real do filme na BD
                    JaViu = jaViu,
                    Data = DateTime.Now
                });

                // new entry and it's marked as watched -> should process
                if (jaViu) shouldProcessDesafio = true;
            }

            await _context.SaveChangesAsync();

            // If the movie became watched, update desafios progress for the user
            if (shouldProcessDesafio)
            {
                await HandleDesafioProgressAsync(userId, dbFilme);
            }

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
                totalHoras = movies.Sum(m => m.Filme.Duracao) / 60.0, // Mantido para compatibilidade
                totalMinutos = movies.Sum(m => m.Filme.Duracao), // Novo campo em minutos
                generos = movies
                    .GroupBy(m => m.Filme.Genero)
                    .Select(g => new { genero = g.Key, total = g.Count() })
                    .OrderByDescending(x => x.total)
                    .ToList()
            });
        }

        // When a user watches a movie, update matching active desafios progress for that user.
        private async Task HandleDesafioProgressAsync(string userId, Filme filme)
        {
            if (filme == null || string.IsNullOrWhiteSpace(filme.Genero)) return;

            var now = DateTime.Now;

            // Find active desafios that match the film genre (case-insensitive) and are in the current date window
            var matchingDesafios = await _context.Desafios
                .Where(d =>
                    d.Ativo
                    && !string.IsNullOrEmpty(d.Genero)
                    && d.DataInicio <= now && d.DataFim >= now
                    && d.Genero.ToLower() == filme.Genero.ToLower()
                )
                .ToListAsync();

            if (matchingDesafios == null || matchingDesafios.Count == 0) return;

            bool anyChange = false;

            foreach (var desafio in matchingDesafios)
            {
                // Load user desafio
                var userDesafio = await _context.UserDesafios
                    .FirstOrDefaultAsync(ud => ud.UtilizadorId == userId && ud.DesafioId == desafio.Id);

                if (userDesafio == null)
                {
                    userDesafio = new UserDesafio
                    {
                        UtilizadorId = userId,
                        DesafioId = desafio.Id,
                        QuantidadeProgresso = 0,
                        DataAtualizacao = DateTime.Now
                    };
                    _context.UserDesafios.Add(userDesafio);
                }

                // If already completed, skip
                if (userDesafio.QuantidadeProgresso >= desafio.QuantidadeNecessaria) continue;

                // Increment progress by 1 (one movie)
                userDesafio.QuantidadeProgresso += 1;
                userDesafio.DataAtualizacao = DateTime.Now;
                anyChange = true;

                // If just completed, award XP once
                if (userDesafio.QuantidadeProgresso >= desafio.QuantidadeNecessaria)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        // Award XP only once when crossing threshold
                        user.XP += desafio.Xp;
                    }
                }
            }

            if (anyChange)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
