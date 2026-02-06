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

        [HttpPost("add")]
        public async Task<IActionResult> AddMovie(int filmeId, bool jaViu)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            Filme? dbFilme = await _context.Set<Filme>().FindAsync(filmeId);
            
            if (dbFilme == null)
            {
                var seedFilme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == filmeId);
                if (seedFilme == null)
                    return BadRequest("Filme inválido (não existe no catálogo).");

                if (!string.IsNullOrEmpty(seedFilme.TmdbId))
                {
                    dbFilme = await _context.Set<Filme>()
                        .FirstOrDefaultAsync(f => f.TmdbId == seedFilme.TmdbId);
                }
                
                if (dbFilme == null)
                {
                    dbFilme = await _context.Set<Filme>()
                        .FirstOrDefaultAsync(f => f.Titulo == seedFilme.Titulo);
                }
                
                if (dbFilme == null)
                {
                    dbFilme = new Filme
                    {
                        Titulo = seedFilme.Titulo,
                        Duracao = seedFilme.Duracao,
                        Genero = seedFilme.Genero,
                        PosterUrl = seedFilme.PosterUrl ?? "",
                        TmdbId = seedFilme.TmdbId ?? ""
                    };

                    _context.Set<Filme>().Add(dbFilme);
                    await _context.SaveChangesAsync();
                }
            }

            var actualFilmeId = dbFilme.Id;
            var existing = await _context.UserMovies
                .FirstOrDefaultAsync(um => um.UtilizadorId == userId && um.FilmeId == actualFilmeId);

            bool shouldProcessDesafio = false;
            if (existing != null)
            {
                var previouslyWatched = existing.JaViu;
                if (!previouslyWatched && jaViu)
                {
                    shouldProcessDesafio = true;
                }

                existing.JaViu = jaViu;
                existing.Data = DateTime.Now;
            }
            else
            {
                _context.UserMovies.Add(new UserMovie
                {
                    UtilizadorId = userId,
                    FilmeId = actualFilmeId,
                    JaViu = jaViu,
                    Data = DateTime.Now
                });

                if (jaViu) shouldProcessDesafio = true;
            }

            await _context.SaveChangesAsync();

            if (shouldProcessDesafio)
            {
                await HandleDesafioProgressAsync(userId, dbFilme);
            }

            return Ok();
        }

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

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var movies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .ToListAsync();

            var generosPorNome = CountByIndividualGenreTyped(movies)
                .Select(g => new { genero = g.genero, total = g.total }).ToList();

            return Ok(new
            {
                totalFilmes = movies.Count,
                totalHoras = movies.Sum(m => m.Filme.Duracao) / 60.0,
                totalMinutos = movies.Sum(m => m.Filme.Duracao),
                generos = generosPorNome
            });
        }

        [HttpGet("stats/comparison")]
        public async Task<IActionResult> GetStatsComparison()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var userMovies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .ToListAsync();

            var userTotalFilmes = userMovies.Count;
            var userTotalMinutos = userMovies.Sum(m => m.Filme?.Duracao ?? 0);
            var userTotalHoras = userTotalMinutos / 60.0;

            var userGeneros = CountByIndividualGenreTyped(userMovies)
                .Select(g => new { genero = g.genero, total = g.total }).ToList();

            var allUserIds = await _context.UserMovies
                .Select(um => um.UtilizadorId)
                .Distinct()
                .ToListAsync();

            var totalUsers = allUserIds.Count;
            if (totalUsers == 0) totalUsers = 1;

            var allWatchedMovies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.JaViu)
                .ToListAsync();

            var globalTotalFilmes = allWatchedMovies.Count;
            var globalTotalMinutos = allWatchedMovies.Sum(m => m.Filme?.Duracao ?? 0);

            var avgFilmesPerUser = (double)globalTotalFilmes / totalUsers;
            var avgMinutosPerUser = (double)globalTotalMinutos / totalUsers;
            var avgHorasPerUser = avgMinutosPerUser / 60.0;

            var globalGenerosRaw = CountByIndividualGenreTyped(allWatchedMovies).Take(10).ToList();
            var totalUserGenreHits = userGeneros.Sum(g => g.total);
            var totalGlobalGenreHits = globalGenerosRaw.Sum(g => g.total);

            var globalGeneros = globalGenerosRaw.Select(g => new {
                genero = g.genero,
                total = g.total,
                percentagem = totalGlobalGenreHits > 0 ? Math.Round((double)g.total / totalGlobalGenreHits * 100, 1) : 0
            }).ToList();

            var userGenerosComPercentagem = userGeneros.Select(g => new {
                g.genero,
                g.total,
                percentagem = totalUserGenreHits > 0 ? Math.Round((double)g.total / totalUserGenreHits * 100, 1) : 0
            }).ToList();

            return Ok(new
            {
                user = new
                {
                    totalFilmes = userTotalFilmes,
                    totalHoras = Math.Round(userTotalHoras, 1),
                    totalMinutos = userTotalMinutos,
                    generos = userGenerosComPercentagem
                },
                global = new
                {
                    totalUtilizadores = totalUsers,
                    mediaFilmesPorUtilizador = Math.Round(avgFilmesPerUser, 1),
                    mediaHorasPorUtilizador = Math.Round(avgHorasPerUser, 1),
                    mediaMinutosPorUtilizador = Math.Round(avgMinutosPerUser, 0),
                    generos = globalGeneros
                },
                comparacao = new
                {
                    filmesVsMedia = userTotalFilmes - avgFilmesPerUser,
                    horasVsMedia = Math.Round(userTotalHoras - avgHorasPerUser, 1),
                    filmesMaisQueMedia = userTotalFilmes > avgFilmesPerUser,
                    horasMaisQueMedia = userTotalHoras > avgHorasPerUser,
                    percentilFilmes = totalUsers > 1 ? CalculatePercentile(userId, allUserIds, allWatchedMovies) : 100
                }
            });
        }

        private static List<(string genero, int total)> CountByIndividualGenreTyped(IEnumerable<UserMovie> movies)
        {
            return movies
                .Where(m => m.Filme != null && !string.IsNullOrWhiteSpace(m.Filme.Genero))
                .SelectMany(m => m.Filme.Genero
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s)))
                .GroupBy(g => g)
                .Select(g => (g.Key, g.Count()))
                .OrderByDescending(x => x.Item2)
                .ToList();
        }

        private int CalculatePercentile(string userId, List<string> allUserIds, List<UserMovie> allWatchedMovies)
        {
            var userCounts = allUserIds
                .Select(uid => new { 
                    UserId = uid, 
                    Count = allWatchedMovies.Count(m => m.UtilizadorId == uid) 
                })
                .OrderBy(x => x.Count)
                .ToList();

            var currentUserCount = userCounts.FirstOrDefault(x => x.UserId == userId)?.Count ?? 0;
            var usersWithLessOrEqual = userCounts.Count(x => x.Count <= currentUserCount);

            return (int)Math.Round((double)usersWithLessOrEqual / userCounts.Count * 100);
        }

        [HttpGet("stats/charts")]
        public async Task<IActionResult> GetStatsCharts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var movies = await _context.UserMovies
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .ToListAsync();

            var generos = CountByIndividualGenreTyped(movies).Take(12)
                .Select(g => new { genero = g.genero, total = g.total }).ToList();

            var now = DateTime.Now;
            var twelveMonthsAgo = now.AddMonths(-12);
            var porMes = movies
                .Where(m => m.Data >= twelveMonthsAgo)
                .GroupBy(m => new { m.Data.Year, m.Data.Month })
                .Select(g => new
                {
                    ano = g.Key.Year,
                    mes = g.Key.Month,
                    label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("pt-PT")),
                    total = g.Count()
                })
                .OrderBy(x => x.ano).ThenBy(x => x.mes)
                .ToList();

            var totalFilmes = movies.Count;
            var totalMinutos = movies.Sum(m => m.Filme?.Duracao ?? 0);

            return Ok(new
            {
                generos,
                porMes,
                resumo = new { totalFilmes, totalHoras = Math.Round(totalMinutos / 60.0, 1), totalMinutos }
            });
        }

        private async Task HandleDesafioProgressAsync(string userId, Filme filme)
        {
            if (filme == null || string.IsNullOrWhiteSpace(filme.Genero)) return;

            var now = DateTime.Now;

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

                if (userDesafio.QuantidadeProgresso >= desafio.QuantidadeNecessaria) continue;

                userDesafio.QuantidadeProgresso += 1;
                userDesafio.DataAtualizacao = DateTime.Now;
                anyChange = true;

                if (userDesafio.QuantidadeProgresso >= desafio.QuantidadeNecessaria)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
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
