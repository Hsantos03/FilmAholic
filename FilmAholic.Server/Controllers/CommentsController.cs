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
    /// Fornecedor assíncrono de operações elementares (CRUD) de crítica individual.
    /// Exclusivamente atrelado à publicação de pequenos parágrafos e reações numéticas diretamente numa Movie Page.
    /// </summary>
    [ApiController]
    [Route("api/comments")]
    public class CommentsController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;
        private readonly ILogger<CommentsController> _logger;

        /// <summary>
        /// Injeta repositório EF e Logger para as gravações e monitorização no pipeline de logs.
        /// </summary>
        /// <param name="context">Base de dados SQlite.</param>
        /// <param name="logger">Output stream para consola e Application Insights.</param>
        public CommentsController(FilmAholicDbContext context, ILogger<CommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Devolve a cascata de comentários para ser pintada num scroll debaixo da sinopse da pelicula.
        /// Anexa Tags de recompensa passivas e Links de Perfil para a Interface.
        /// </summary>
        /// <param name="movieId">Primary Id representativo do Filme no TMDB.</param>
        /// <param name="page">Indíce numérico da listagem pedida.</param>
        /// <param name="pageSize">Densidade de conteúdo a cuspir nesta iteração temporal.</param>
        /// <returns>Fragmento paginado e formatado em propriedades contendo a estrutura CommentDTOs e métricas relativas aos corações que recebeu.</returns>
        [HttpGet("movie/{movieId:int}")]
        public async Task<IActionResult> GetByMovie(int movieId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            try
            {
                var userId = User.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "")
                    : null;

                var query = _context.Comments.Where(c => c.FilmeId == movieId);
                var totalCount = await query.CountAsync();

                var comments = await query
                    .OrderByDescending(c => c.DataCriacao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var commentIds = comments.Select(c => c.Id).ToList();
                // ... same logic as before for DTO generation ...
                var userIds = comments.Select(c => c.UserId).Where(uid => uid != null).Distinct().ToList();
                Dictionary<string, string?> userNameByUserId = new();
                Dictionary<string, string?> fotoByUserId = new();
                Dictionary<string, string?> userTagByUserId = new();
                Dictionary<string, string?> userTagDescByUserId = new();
                Dictionary<string, string?> userTagIconByUserId = new();
                Dictionary<string, string?> userTagPrimaryColorByUserId = new();
                Dictionary<string, string?> userTagSecondaryColorByUserId = new();
                if (userIds.Count > 0)
                {
                    var userData = await _context.Set<Utilizador>()
                        .Where(u => userIds.Contains(u.Id))
                        .Select(u => new { u.Id, u.UserName, u.Nome, u.Sobrenome, u.FotoPerfilUrl, u.UserTag, u.UserTagPrimaryColor, u.UserTagSecondaryColor })
                        .ToListAsync();
                    userNameByUserId = userData.ToDictionary(x => x.Id, x => !string.IsNullOrEmpty(x.UserName) && !x.UserName.Contains("@") ? x.UserName : x.Nome + " " + x.Sobrenome);
                    fotoByUserId = userData.ToDictionary(x => x.Id, x => x.FotoPerfilUrl);
                    userTagByUserId = userData.ToDictionary(x => x.Id, x => x.UserTag);
                    userTagPrimaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagPrimaryColor);
                    userTagSecondaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagSecondaryColor);

                    // Fetch medal descriptions and icons for user tags (case-insensitive)
                    var tags = userData.Select(x => x.UserTag).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                    var medalData = await _context.Medalhas
                        .ToListAsync();
                    var descByTag = medalData.ToDictionary(
                        x => x.Nome,
                        x => x.Descricao,
                        StringComparer.OrdinalIgnoreCase);
                    var iconByTag = medalData.ToDictionary(
                        x => x.Nome,
                        x => x.IconeUrl,
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var user in userData)
                    {
                        if (!string.IsNullOrEmpty(user.UserTag))
                        {
                            if (descByTag.TryGetValue(user.UserTag, out var desc))
                            {
                                userTagDescByUserId[user.Id] = desc;
                            }
                            if (iconByTag.TryGetValue(user.UserTag, out var icon))
                            {
                                userTagIconByUserId[user.Id] = icon;
                            }
                        }
                    }
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
                    UserId = c.UserId,
                    UserName = ResolveCommentListUserName(c, userNameByUserId),
                    FotoPerfilUrl = c.UserId != null && fotoByUserId.TryGetValue(c.UserId, out var url) ? url : null,
                    UserTag = c.UserId != null && userTagByUserId.TryGetValue(c.UserId, out var tag) ? tag : null,
                    UserTagDescription = c.UserId != null && userTagDescByUserId.TryGetValue(c.UserId, out var tagDesc) ? tagDesc : null,
                    UserTagIconUrl = c.UserId != null && userTagIconByUserId.TryGetValue(c.UserId, out var tagIcon) ? tagIcon : null,
                    UserTagPrimaryColor = c.UserId != null && userTagPrimaryColorByUserId.TryGetValue(c.UserId, out var tagPrimary) ? tagPrimary : null,
                    UserTagSecondaryColor = c.UserId != null && userTagSecondaryColorByUserId.TryGetValue(c.UserId, out var tagSecondary) ? tagSecondary : null,
                    Texto = c.Texto,
                    DataCriacao = c.DataCriacao,
                    DataEdicao = c.DataEdicao,
                    CanEdit = userId != null && c.UserId == userId,
                    LikeCount = likesByComment.TryGetValue(c.Id, out var lc) ? lc : 0,
                    DislikeCount = dislikesByComment.TryGetValue(c.Id, out var dc) ? dc : 0,
                    MyVote = myVoteByComment.TryGetValue(c.Id, out var mv) ? mv : 0
                }).ToList();

                return Ok(new PaginatedCommentsDTO { Comments = dtos, TotalCount = totalCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter comentários do filme {MovieId}.", movieId);
                return Ok(new { comments = new List<CommentDTO>(), totalCount = 0 });
            }
        }

        /// <summary>
        /// Mostra uma descrição sobre a obra e constrói a associação entre utilizador / TMDb ID efetuando guardas defensivas sobre submissões nulas.
        /// Resgata o Username original pelo Identity NameIdentifier.
        /// </summary>
        /// <param name="dto">Recetáculo que abarca o Movie ID e o String cru do texto.</param>
        /// <returns>Uma cópia perfeitamente mapeada 201 com o novo timestamp e medalha atrelada se obtida.</returns>
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
                    if (!string.IsNullOrEmpty(user.UserName) && !user.UserName.Contains("@"))
                        displayName = user.UserName;
                    else
                        displayName = $"{user.Nome?.Trim() ?? ""} {user.Sobrenome?.Trim() ?? ""}".Trim();
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

                // Get medal description and icon for the tag (case-insensitive)
                string? tagDescription = null;
                string? tagIconUrl = null;
                if (!string.IsNullOrEmpty(user?.UserTag))
                {
                    var medal = await _context.Medalhas
                        .FirstOrDefaultAsync(m => m.Nome.ToLower() == user.UserTag.ToLower());
                    tagDescription = medal?.Descricao;
                    tagIconUrl = medal?.IconeUrl;
                }

                var outDto = new CommentDTO
                {
                    Id = comment.Id,
                    UserId = comment.UserId,
                    UserName = comment.UserName,
                    FotoPerfilUrl = user?.FotoPerfilUrl,
                    UserTag = user?.UserTag,
                    UserTagDescription = tagDescription,
                    UserTagIconUrl = tagIconUrl,
                    UserTagPrimaryColor = user?.UserTagPrimaryColor,
                    UserTagSecondaryColor = user?.UserTagSecondaryColor,
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

        /// <summary>
        /// Substitui uma reflexão existente a quem invoca a Primary Key de Autor nos conformes de Policy Authorize.
        /// Atualiza também o timestamp local "DataEdicao" como alterado.
        /// </summary>
        /// <param name="id">Índice relacional (Identidade da table Comment).</param>
        /// <param name="dto">O novo pedaço textual cru a ser enxertado.</param>
        /// <returns>Novo objeto renderizado após recomputação no EntityFramework, guardando e preservando Likes Count.</returns>
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

            var commentUser = await _context.Set<Utilizador>().FindAsync(comment.UserId);
            var fotoUrl = commentUser?.FotoPerfilUrl;
            var userTag = commentUser?.UserTag;

            // Get medal description and icon (case-insensitive)
            string? tagDescription = null;
            string? tagIconUrl = null;
            if (!string.IsNullOrEmpty(userTag))
            {
                var medal = await _context.Medalhas
                    .FirstOrDefaultAsync(m => m.Nome.ToLower() == userTag.ToLower());
                tagDescription = medal?.Descricao;
                tagIconUrl = medal?.IconeUrl;
            }

            return Ok(new CommentDTO
            {
                Id = comment.Id,
                UserId = comment.UserId,
                UserName = commentUser != null
                    ? (!string.IsNullOrEmpty(commentUser.UserName) && !commentUser.UserName.Contains("@") ? commentUser.UserName : commentUser.Nome + " " + commentUser.Sobrenome)
                    : (string.IsNullOrEmpty(comment.UserId) ? comment.UserName : "Conta Eliminada"),
                FotoPerfilUrl = fotoUrl,
                UserTag = userTag,
                UserTagDescription = tagDescription,
                UserTagIconUrl = tagIconUrl,
                UserTagPrimaryColor = commentUser?.UserTagPrimaryColor,
                UserTagSecondaryColor = commentUser?.UserTagSecondaryColor,
                Texto = comment.Texto,
                DataCriacao = comment.DataCriacao,
                DataEdicao = comment.DataEdicao,
                CanEdit = true,
                LikeCount = likeCount,
                DislikeCount = dislikeCount,
                MyVote = myVote
            });
        }

        /// <summary>
        /// Remove o input do fórum/crítica caso se pertença à instância referenciada.
        /// </summary>
        /// <param name="id">Índice estrito para procurar no tracking DbContext.</param>
        /// <returns>HTTP Final 204 No Content confirmando vazio com aval de transação.</returns>
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

        /// <summary>
        /// Vota Like/Dislike. Apaga se o utilizador refizer toggle sobre ele próprio.
        /// Revalida a totalidade pós ação.
        /// </summary>
        /// <param name="id">A Primary Key original da tabela associada à obra a intercetar.</param>
        /// <param name="dto">Atributo reencaminhador com '1', '-1' ou '0'.</param>
        /// <returns>Espelho sumário (CreateCommentDTO pseudo) mas englobado para dar reset às barras estatísticas no Angular após Toggle.</returns>
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

        private static string ResolveCommentListUserName(Comments c, Dictionary<string, string?> userNameByUserId)
        {
            if (!string.IsNullOrEmpty(c.UserId) &&
                userNameByUserId.TryGetValue(c.UserId, out var resolved) &&
                !string.IsNullOrEmpty(resolved))
                return resolved;
            if (string.IsNullOrEmpty(c.UserId))
                return string.IsNullOrEmpty(c.UserName) ? "Conta Eliminada" : c.UserName;
            return "Conta Eliminada";
        }
    }
}
