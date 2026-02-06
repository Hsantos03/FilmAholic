using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/movieratings")]
    public class MovieRatingsController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public MovieRatingsController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // GET api/movieratings/5
        // devolve média, total votos, e o voto do utilizador (se autenticado)
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

        // PUT api/movieratings/5  body: { score: 0..10 }
        // cria ou atualiza o voto do utilizador (1 voto por filme)
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

        // DELETE api/movieratings/5  -> remove o teu voto
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
