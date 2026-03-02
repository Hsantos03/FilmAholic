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
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(FilmAholicDbContext context, ILogger<CommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("movie/{movieId:int}")]
        public async Task<ActionResult<List<CommentDTO>>> GetByMovie(int movieId)
        {
            try
            {
                var userId = User.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "")
                    : null;

                var comments = await _context.Comments
                    .Where(c => c.FilmeId == movieId)
                    .OrderByDescending(c => c.DataCriacao)
                    .ToListAsync();

                var commentIds = comments.Select(c => c.Id).ToList();

                var userIds = comments.Select(c => c.UserId).Distinct().ToList();
                Dictionary<string, string?> fotoByUserId = new();
                if (userIds.Count > 0)
                {
                    var userFotos = await _context.Set<Utilizador>()
                        .Where(u => userIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.FotoPerfilUrl })
                        .ToListAsync();
                    fotoByUserId = userFotos.ToDictionary(x => x.Id, x => x.FotoPerfilUrl);
                }

                var likesByComment = new Dictionary<int, int>();
                var dislikesByComment = new Dictionary<int, int>();
                if (commentIds.Count > 0)
                {
                    var voteAgg = await _context.CommentVotes
                        .Where(v => commentIds.Contains(v.CommentId))
                        .GroupBy(v => v.CommentId)
                        .Select(g => new { CommentId = g.Key, Likes = g.Count(x => x.IsLike), Dislikes = g.Count(x => !x.IsLike) })
                        .ToListAsync();
                    likesByComment = voteAgg.ToDictionary(x => x.CommentId, x => x.Likes);
                    dislikesByComment = voteAgg.ToDictionary(x => x.CommentId, x => x.Dislikes);
                }

                Dictionary<int, int> myVoteByComment = new();
                if (!string.IsNullOrWhiteSpace(userId) && commentIds.Count > 0)
                {
                    var myVotes = await _context.CommentVotes
                        .Where(v => commentIds.Contains(v.CommentId) && v.UserId == userId)
                        .ToListAsync();
                    myVoteByComment = myVotes.ToDictionary(v => v.CommentId, v => v.IsLike ? 1 : -1);
                }

                var dtos = comments.Select(c => new CommentDTO
                {
                    Id = c.Id,
                    UserName = c.UserName,
                    FotoPerfilUrl = fotoByUserId.TryGetValue(c.UserId, out var url) ? url : null,
                    Texto = c.Texto,
                    DataCriacao = c.DataCriacao,
                    DataEdicao = c.DataEdicao,
                    CanEdit = userId != null && c.UserId == userId,
                    LikeCount = likesByComment.TryGetValue(c.Id, out var lc) ? lc : 0,
                    DislikeCount = dislikesByComment.TryGetValue(c.Id, out var dc) ? dc : 0,
                    MyVote = myVoteByComment.TryGetValue(c.Id, out var mv) ? mv : 0
                }).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter comentários do filme {MovieId}. Pode faltar aplicar migrações na base de dados.", movieId);
                // Devolver lista vazia para a página do filme carregar; o utilizador vê "sem comentários"
                return Ok(new List<CommentDTO>());
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<CommentDTO>> Create([FromBody] CreateCommentDTO dto)
        {
            try
            {
                if (dto.FilmeId <= 0) return BadRequest("Filme inválido.");
                if (string.IsNullOrWhiteSpace(dto.Texto)) return BadRequest("Texto obrigatório.");

                var filmeExists = await _context.Filmes.AnyAsync(f => f.Id == dto.FilmeId);
                if (!filmeExists) return NotFound("Filme não encontrado.");

                var userId =
                    User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("sub") ??
                    User.FindFirstValue("id");

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized("Utilizador não autenticado.");

                var displayName = "User";
                var user = await _context.Set<Utilizador>().FindAsync(userId);
                if (user != null)
                {
                    var nomeCompleto = $"{user.Nome?.Trim() ?? ""} {user.Sobrenome?.Trim() ?? ""}".Trim();
                    if (!string.IsNullOrEmpty(nomeCompleto))
                        displayName = nomeCompleto;
                    else if (!string.IsNullOrEmpty(user.UserName) && !user.UserName.Contains("@"))
                        displayName = user.UserName;
                }

                if (displayName == "User")
                {
                    var fromClaims = User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("name");
                    if (!string.IsNullOrEmpty(fromClaims) && !fromClaims.Contains("@"))
                        displayName = fromClaims;
                }

                var comment = new Comments
                {
                    FilmeId = dto.FilmeId,
                    Texto = dto.Texto.Trim(),
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
                    FotoPerfilUrl = user?.FotoPerfilUrl,
                    Texto = comment.Texto,
                    DataCriacao = comment.DataCriacao,
                    CanEdit = true,
                    LikeCount = 0,
                    DislikeCount = 0,
                    MyVote = 0
                };

                return Ok(outDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar comentário para o filme {FilmeId}", dto?.FilmeId);
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Erro ao publicar comentário. Tente novamente.", detail });
            }
        }

        [Authorize]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<CommentDTO>> Update(int id, [FromBody] CreateCommentDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Texto)) return BadRequest("Texto obrigatório.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null) return NotFound("Comentário não encontrado.");
            if (comment.UserId != userId) return Forbid();

            comment.Texto = dto.Texto.Trim();
            comment.DataEdicao = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var likeCount = await _context.CommentVotes.CountAsync(v => v.CommentId == id && v.IsLike);
            var dislikeCount = await _context.CommentVotes.CountAsync(v => v.CommentId == id && !v.IsLike);
            var myVote = await _context.CommentVotes
                .Where(v => v.CommentId == id && v.UserId == userId)
                .Select(v => v.IsLike ? 1 : -1)
                .FirstOrDefaultAsync();

            var commentUser = await _context.Users.FindAsync(comment.UserId);
            var fotoUrl = (commentUser as Utilizador)?.FotoPerfilUrl;

            return Ok(new CommentDTO
            {
                Id = comment.Id,
                UserName = comment.UserName,
                FotoPerfilUrl = fotoUrl,
                Texto = comment.Texto,
                DataCriacao = comment.DataCriacao,
                DataEdicao = comment.DataEdicao,
                CanEdit = true,
                LikeCount = likeCount,
                DislikeCount = dislikeCount,
                MyVote = myVote
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

        [Authorize]
        [HttpPost("{id:int}/vote")]
        public async Task<ActionResult<CreateCommentDTO>> Vote(int id, [FromBody] CreateCommentDTO dto)
        {
            if (dto.Value != 1 && dto.Value != -1 && dto.Value != 0)
                return BadRequest("Value tem de ser 1 (like), -1 (dislike) ou 0 (remover).");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var commentExists = await _context.Comments.AnyAsync(c => c.Id == id);
            if (!commentExists) return NotFound("Comentário não encontrado.");

            var existing = await _context.CommentVotes
                .FirstOrDefaultAsync(v => v.CommentId == id && v.UserId == userId);

            if (dto.Value == 0)
            {
                if (existing != null)
                {
                    _context.CommentVotes.Remove(existing);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var isLike = dto.Value == 1;

                if (existing == null)
                {
                    _context.CommentVotes.Add(new CommentVote
                    {
                        CommentId = id,
                        UserId = userId,
                        IsLike = isLike,
                        DataCriacao = DateTime.UtcNow,
                        DataAtualizacao = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.IsLike = isLike;
                    existing.DataAtualizacao = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            var likeCount = await _context.CommentVotes.CountAsync(v => v.CommentId == id && v.IsLike);
            var dislikeCount = await _context.CommentVotes.CountAsync(v => v.CommentId == id && !v.IsLike);
            var myVote = await _context.CommentVotes
                .Where(v => v.CommentId == id && v.UserId == userId)
                .Select(v => v.IsLike ? 1 : -1)
                .FirstOrDefaultAsync();

            return Ok(new CreateCommentDTO
            {
                LikeCount = likeCount,
                DislikeCount = dislikeCount,
                MyVote = myVote
            });
        }
    }
}
