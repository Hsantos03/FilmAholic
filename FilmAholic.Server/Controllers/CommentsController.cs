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

        [HttpGet("movie/{movieId:int}")]
        public async Task<ActionResult<List<CommentDTO>>> GetByMovie(int movieId)
        {
            var userId = User.Identity?.IsAuthenticated == true
                ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "")
                : null;

            var comments = await _context.Comments
                .Where(c => c.FilmeId == movieId)
                .OrderByDescending(c => c.DataCriacao)
                .ToListAsync();

            var userIds = comments.Select(c => c.UserId).Distinct().ToList();
            var userFotos = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, Foto = ((Utilizador)u).FotoPerfilUrl })
                .ToListAsync();
            var fotoByUserId = userFotos.ToDictionary(x => x.Id, x => x.Foto);

            var dtos = comments.Select(c => new CommentDTO
            {
                Id = c.Id,
                UserName = c.UserName,
                FotoPerfilUrl = fotoByUserId.TryGetValue(c.UserId, out var url) ? url : null,
                Texto = c.Texto,
                Rating = c.Rating,
                DataCriacao = c.DataCriacao,
                CanEdit = userId != null && c.UserId == userId
            }).ToList();

            return Ok(dtos);
        }

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

            var displayName = "User";
            var user = await _context.Users.FindAsync(userId);
            if (user is Utilizador u)
            {
                var nomeCompleto = $"{u.Nome?.Trim() ?? ""} {u.Sobrenome?.Trim() ?? ""}".Trim();
                if (!string.IsNullOrEmpty(nomeCompleto))
                    displayName = nomeCompleto;
                else if (!string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@"))
                    displayName = u.UserName;
            }
            if (displayName == "User")
            {
                var fromClaims = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
                if (!string.IsNullOrEmpty(fromClaims) && !fromClaims.Contains("@"))
                    displayName = fromClaims;
            }

            var comment = new Comment
            {
                FilmeId = dto.FilmeId,
                Texto = dto.Texto.Trim(),
                Rating = dto.Rating,
                UserId = userId,
                UserName = displayName,
                DataCriacao = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var outDto = new CommentDTO
            {
                Id = comment.Id,
                UserName = comment.UserName,
                FotoPerfilUrl = user is Utilizador u2 ? u2.FotoPerfilUrl : null,
                Texto = comment.Texto,
                Rating = comment.Rating,
                DataCriacao = comment.DataCriacao
            };

            return Ok(outDto);
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CommentDTO>> Update(int id, [FromBody] UpdateCommentDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Texto)) return BadRequest("Texto obrigatório.");
            if (dto.Rating < 1 || dto.Rating > 5) return BadRequest("Rating tem de ser 1..5.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comentário não encontrado.");
            if (comment.UserId != userId) return Forbid();

            comment.Texto = dto.Texto.Trim();
            comment.Rating = dto.Rating;
            await _context.SaveChangesAsync();

            var commentUser = await _context.Users.FindAsync(comment.UserId);
            var fotoUrl = (commentUser as Utilizador)?.FotoPerfilUrl;

            return Ok(new CommentDTO
            {
                Id = comment.Id,
                UserName = comment.UserName,
                FotoPerfilUrl = fotoUrl,
                Texto = comment.Texto,
                Rating = comment.Rating,
                DataCriacao = comment.DataCriacao,
                CanEdit = true
            });
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comentário não encontrado.");
            if (comment.UserId != userId) return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
