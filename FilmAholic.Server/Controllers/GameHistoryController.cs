using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/game/history")]
    public class GameHistoryController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public GameHistoryController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // GET: api/game/history
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var history = await _context.Set<GameHistory>()
                .Where(h => h.UtilizadorId == userId)
                .OrderByDescending(h => h.DataCriacao)
                .Take(50)
                .ToListAsync();

            return Ok(history);
        }

        // POST: api/game/history
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SaveResult([FromBody] GameHistoryCreateDto dto)
        {
            if (dto == null) return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var entity = new GameHistory
            {
                UtilizadorId = userId,
                Score = dto.Score,
                RoundsJson = dto.RoundsJson ?? string.Empty,
                DataCriacao = DateTime.UtcNow
            };

            _context.Set<GameHistory>().Add(entity);
            await _context.SaveChangesAsync();

            return Ok(entity);
        }
    }

    public class GameHistoryCreateDto
    {
        public int Score { get; set; }
        public string? RoundsJson { get; set; }
    }
}