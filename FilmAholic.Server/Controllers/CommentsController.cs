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
    [Route("api/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public CommentsController(FilmAholicDbContext context)
        {
            _context = context;
        }

        // LISTA dos comentários
        [HttpGet("movie/{movieId:int}")]
        public async Task<ActionResult<List<CommentDTO>>> GetByMovie(int movieId)
        {
            var comments = await _context.Comments
                .Where(c => c.FilmeId == movieId)
                .OrderByDescending(c => c.DataCriacao)
                .Select(c => new CommentDTO
                {
                    Id = c.Id,
                    UserName = c.UserName,
                    Texto = c.Texto,
                    Rating = c.Rating,
                    DataCriacao = c.DataCriacao
                })
                .ToListAsync();

            return Ok(comments);
        }

        // Comentário só com conta criada
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentDTO>> Create([FromBody] CreateCommentDTO dto)
        {
            if (dto.FilmeId <= 0) return BadRequest("Filme inválido.");
            if (string.IsNullOrWhiteSpace(dto.Texto)) return BadRequest("Texto obrigatório.");
            if (dto.Rating < 1 || dto.Rating > 5) return BadRequest("Rating tem de ser 1..5.");

            var filmeExists = await _context.Filmes.AnyAsync(f => f.Id == dto.FilmeId);
            if (!filmeExists) return NotFound("Filme não encontrado.");

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("Utilizador não autenticado.");

            var userName =
                User.Identity?.Name ??
                User.FindFirstValue(ClaimTypes.Name) ??
                User.FindFirstValue("name") ??
                "User";

            var comment = new Comment
            {
                FilmeId = dto.FilmeId,
                Texto = dto.Texto.Trim(),
                Rating = dto.Rating,
                UserId = userId,
                UserName = userName,
                DataCriacao = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var outDto = new CommentDTO
            {
                Id = comment.Id,
                UserName = comment.UserName,
                Texto = comment.Texto,
                Rating = comment.Rating,
                DataCriacao = comment.DataCriacao
            };

            return Ok(outDto);
        }
    }
}
