using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Controlador gerador e distribuidor de avaliações e classificações numéricas (ratings).
    /// Regista qual a pontuação dos vários utilizadores face às longas metragens.
    /// </summary>
    [ApiController]
    [Route("api/movieratings")]
    public class MovieRatingsController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public MovieRatingsController(FilmAholicDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtém a cotação global do filme correspondente, o total de votos aglomerados e o voto específico se o utilizador estiver em sessão.
        /// </summary>
        /// <param name="movieId">Identificação primária do filme em território interno e na base de dados.</param>
        /// <returns>Transfere um resumo das estatísticas sob forma do objeto <see cref="MovieRatingDTO"/>.</returns>
        [HttpGet("{movieId:int}")]
        public async Task<ActionResult<MovieRatingDTO>> Get(int movieId)
        {
            var exists = await _context.Filmes.AnyAsync(f => f.Id == movieId);
            if (!exists) return NotFound("Filme não encontrado.");

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("sub") ??
                    User.FindFirstValue("id");
            }

            var query = _context.MovieRatings.Where(r => r.FilmeId == movieId);

            var count = await query.CountAsync();
            var average = count > 0 ? await query.AverageAsync(r => (double)r.Score) : 0.0;

            int? userScore = null;
            if (!string.IsNullOrWhiteSpace(userId))
            {
                userScore = await query
                    .Where(r => r.UserId == userId)
                    .Select(r => (int?)r.Score)
                    .FirstOrDefaultAsync();
            }

            return Ok(new MovieRatingDTO
            {
                Average = average,
                Count = count,
                UserScore = userScore
            });
        }

        /// <summary>
        /// Cria iterativamente ou ajusta de imediato o voto atual do utilizador para esse filme (limitado intencionalmente a 1).
        /// </summary>
        /// <param name="movieId">ID numérico unívoco da obra cinematográfica alvo.</param>
        /// <param name="dto">O portador com a pontuação de 0 a 10.</param>
        /// <returns>As estatísticas recém-calculadas incorporando o voto em perspetiva.</returns>
        [Authorize]
        [HttpPut("{movieId:int}")]
        public async Task<ActionResult<MovieRatingDTO>> Upsert(int movieId, [FromBody] RatingsDto dto)
        {
            if (dto.Score < 0 || dto.Score > 10)
                return BadRequest("Score tem de ser 0..10.");

            var filmeExists = await _context.Filmes.AnyAsync(f => f.Id == movieId);
            if (!filmeExists) return NotFound("Filme não encontrado.");

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Utilizador não autenticado.");

            var existing = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.FilmeId == movieId && r.UserId == userId);

            if (existing == null)
            {
                existing = new MovieRating
                {
                    FilmeId = movieId,
                    UserId = userId,
                    Score = dto.Score,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.MovieRatings.Add(existing);
            }
            else
            {
                existing.Score = dto.Score;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // devolve o summary atualizado
            return await Get(movieId);
        }

        /// <summary>
        /// Elimina permanentemente o voto deixado pelo utilizador perante o filme parametrizado, deitando fora do somatório a sua pontuação.
        /// </summary>
        /// <param name="movieId">Número da longa-metragem sujeita à ação de remoção.</param>
        /// <returns>Sinal 204 NoContent assim que processada a limpeza.</returns>
        [Authorize]
        [HttpDelete("{movieId:int}")]
        public async Task<IActionResult> Clear(int movieId)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Utilizador não autenticado.");

            var existing = await _context.MovieRatings
                .FirstOrDefaultAsync(r => r.FilmeId == movieId && r.UserId == userId);

            if (existing == null) return NoContent();

            _context.MovieRatings.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
