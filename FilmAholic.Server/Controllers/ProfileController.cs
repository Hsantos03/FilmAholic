using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Controlador centralizado para extração e mutação de dados identitários do indivíduo.
    /// Gere a exclusão de conta e o vínculo de tags e favoritos anexos à entidade ASP.NET User.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;
        private readonly IPreferenciasService _preferenciasService;
        private readonly UserManager<Utilizador> _userManager;

        public ProfileController(
            FilmAholicDbContext context,
            IPreferenciasService preferenciasService,
            UserManager<Utilizador> userManager)
        {
            _context = context;
            _preferenciasService = preferenciasService;
            _userManager = userManager;
        }

        // GET: api/Profile/{id}
        /// <summary>
        /// Devolve a vista detalhada de um perfil público de utilizador pelo seu UUID.
        /// Exclui o Email se o requerente não for dono ou Admin.
        /// </summary>
        /// <param name="id">A Primary Key em base de GUID nativo do Identity.</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var now = DateTimeOffset.UtcNow;
            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    id = u.Id,
                    userName = !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome,
                    nome = u.Nome,
                    sobrenome = u.Sobrenome,
                    email = u.Email,
                    fotoPerfilUrl = u.FotoPerfilUrl,
                    capaUrl = u.CapaUrl,
                    dataCriacao = u.DataCriacao,
                    bio = u.Bio,
                    xp = u.XP,
                    nivel = u.Nivel,
                    contaBloqueada = u.LockoutEnabled && u.LockoutEnd != null && u.LockoutEnd > now,
                    generosFavoritos = u.GenerosFavoritos.Select(ug => new
                    {
                        id = ug.Genero.Id,
                        nome = ug.Genero.Nome
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound();

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var canSeeEmail = !string.IsNullOrEmpty(callerId)
                && (string.Equals(callerId, id, StringComparison.Ordinal)
                    || User.IsInRole("Administrador"));
            if (canSeeEmail)
                return Ok(user);

            return Ok(new
            {
                user.id,
                user.userName,
                user.nome,
                user.sobrenome,
                user.fotoPerfilUrl,
                user.capaUrl,
                user.dataCriacao,
                user.bio,
                user.xp,
                user.nivel,
                user.contaBloqueada,
                user.generosFavoritos
            });
        }

        // PUT: api/Profile/{id}
        /// <summary>
        /// Atualiza os campos modificáveis do perfil do utilizador.
        /// </summary>
        /// <param name="id">Primary Key GUID do utilizador alvo.</param>
        /// <param name="dto">Campos modificáveis que passaram pelo front-end.</param>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                user.UserName = dto.UserName;
                user.NormalizedUserName = dto.UserName.ToUpperInvariant();
            }

            if (dto.Bio is not null)
            {
                user.Bio = dto.Bio;
            }

            if (dto.FotoPerfilUrl is not null)
            {
                user.FotoPerfilUrl = string.IsNullOrWhiteSpace(dto.FotoPerfilUrl) ? null : dto.FotoPerfilUrl;
            }

            if (dto.CapaUrl is not null)
            {
                user.CapaUrl = string.IsNullOrWhiteSpace(dto.CapaUrl) ? null : dto.CapaUrl;
            }

            try
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return StatusCode(500, new { message = "Failed to update profile.", errors = updateResult.Errors });
                }
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Failed to update profile.", detail = ex.Message });
            }
        }

        // DELETE: api/Profile/{id}
        /// <summary>
        /// Exclui a conta do utilizador e todos os dados associados, mantendo apenas posts e reviews com "Conta Eliminada".
        /// </summary>
        /// <param name="id">GuID restrito perfeitamente comparado face aos JWT Claims.</param>
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(callerId) || callerId != id)
                return Forbid();

            var user = await _context.Users
                .Include(u => u.GenerosFavoritos)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            try
            {
                // Delete user-related data
                var userMovies = _context.UserMovies.Where(um => um.UtilizadorId == id);
                _context.UserMovies.RemoveRange(userMovies);

                var utilGen = _context.UtilizadorGeneros.Where(ug => ug.UtilizadorId == id);
                _context.UtilizadorGeneros.RemoveRange(utilGen);

                // Delete movie ratings made by the user
                var movieRatings = _context.MovieRatings.Where(r => r.UserId == id);
                _context.MovieRatings.RemoveRange(movieRatings);

                // Delete comment votes made by the user
                var commentVotes = _context.CommentVotes.Where(v => v.UserId == id);
                _context.CommentVotes.RemoveRange(commentVotes);

                // Update user's comments to show "Conta Eliminada" instead of username
                var userComments = _context.Comments.Where(c => c.UserId == id);
                foreach (var comment in userComments)
                {
                    comment.UserName = "Conta Eliminada";
                    comment.UserId = null; // Remove the user ID reference
                }

                // Delete the user account
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                    return StatusCode(500, new { message = "Failed to delete user.", errors = result.Errors });

                await _context.SaveChangesAsync();
            return NoContent();
        }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting account.", detail = ex.Message });
            }
        }

        // GET: api/Profile/generos
        /// <summary>
        /// Retorna todos os géneros disponíveis na plataforma.
        /// </summary>
        [HttpGet("generos")]
        public async Task<IActionResult> ObterTodosGeneros()
        {
            var generos = await _preferenciasService.ObterTodosGenerosAsync();
            return Ok(generos.Select(g => new { id = g.Id, nome = g.Nome }));
        }

        // GET: api/Profile/{id}/generos-favoritos
        /// <summary>
        /// Retorna os géneros favoritos de um utilizador específico.
        /// </summary>
        [HttpGet("{id}/generos-favoritos")]
        public async Task<IActionResult> ObterGenerosFavoritos(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id é obrigatório." });

            var generos = await _preferenciasService.ObterGenerosFavoritosAsync(id);
            return Ok(generos.Select(g => new { id = g.Id, nome = g.Nome }));
        }

        // PUT: api/Profile/{id}/generos-favoritos
        /// <summary>
        /// Atualiza os géneros favoritos de um utilizador específico.
        /// </summary>
        /// <param name="id">GUID do Alvo.</param>
        /// <param name="dto">As N-IDs provenientes da resposta.</param>
        [HttpPut("{id}/generos-favoritos")]
        public async Task<IActionResult> AtualizarGenerosFavoritos(string id, [FromBody] AtualizarGenerosFavoritosDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id é obrigatório." });

            var utilizadorExiste = await _context.Users.AnyAsync(u => u.Id == id);
            if (!utilizadorExiste)
                return NotFound(new { message = "Utilizador não encontrado." });

            if (dto.GeneroIds != null && dto.GeneroIds.Any())
            {
                foreach (var generoId in dto.GeneroIds)
                {
                    if (!await _preferenciasService.GeneroExisteAsync(generoId))
                        return BadRequest(new { message = $"Género com ID {generoId} não encontrado." });
                }
            }

            try
            {
                await _preferenciasService.AtualizarGenerosFavoritosAsync(id, dto.GeneroIds ?? new List<int>());
                return Ok(new { message = "Géneros favoritos atualizados com sucesso." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erro ao atualizar géneros favoritos.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Retorna os filmes e atores favoritos do utilizador autenticado.
        /// </summary>
        [Authorize]
        [HttpGet("favorites")]
        public async Task<IActionResult> GetFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var filmes = new List<int>();
            var atores = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(user.TopFilmes))
                    filmes = JsonSerializer.Deserialize<List<int>>(user.TopFilmes) ?? new();

                if (!string.IsNullOrWhiteSpace(user.TopAtores))
                    atores = JsonSerializer.Deserialize<List<string>>(user.TopAtores) ?? new();
            }
            catch
            {
                filmes = new();
                atores = new();
            }

            return Ok(new FavoritosDTO
            {
                Filmes = filmes,
                Atores = atores
            });
        }

        /// <summary>
        /// Retorna os filmes e atores favoritos de um utilizador específico.
        /// </summary>
        [Authorize]
        [HttpGet("{id}/favorites")]
        public async Task<IActionResult> GetFavoritesForUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var exists = await _context.Users.AnyAsync(u => u.Id == id);
            if (!exists) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var filmes = new List<int>();
            var atores = new List<string>();

            try
            {
                if (!string.IsNullOrWhiteSpace(user.TopFilmes))
                    filmes = JsonSerializer.Deserialize<List<int>>(user.TopFilmes) ?? new();

                if (!string.IsNullOrWhiteSpace(user.TopAtores))
                    atores = JsonSerializer.Deserialize<List<string>>(user.TopAtores) ?? new();
            }
            catch
            {
                filmes = new();
                atores = new();
            }

            return Ok(new FavoritosDTO
            {
                Filmes = filmes,
                Atores = atores
            });
        }

        /// <summary> Constante fixa do Limite aceitável em guardas anti-bot. </summary>
        private const int MaxFavoritesStored = 50;

        /// <summary>
        /// Atualiza os filmes e atores favoritos do utilizador autenticado.
        /// </summary>
        [Authorize]
        [HttpPut("favorites")]
        public async Task<IActionResult> UpdateFavorites([FromBody] FavoritosDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.TopFilmes = JsonSerializer.Serialize((dto.Filmes ?? new List<int>()).Take(MaxFavoritesStored).ToList());
            user.TopAtores = JsonSerializer.Serialize((dto.Atores ?? new List<string>()).Take(MaxFavoritesStored).ToList());

            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET: api/Profile/tag
        /// <summary>
        /// Retorna a tag do utilizador autenticado juntamente com as cores primária e secundária.
        /// </summary>
        [Authorize]
        [HttpGet("tag")]
        public async Task<IActionResult> GetUserTag()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new { tag = user.UserTag, primaryColor = user.UserTagPrimaryColor, secondaryColor = user.UserTagSecondaryColor });
        }

        // PUT: api/Profile/tag
        /// <summary>
        /// Atualiza a tag do utilizador autenticado juntamente com as cores primária e secundária.
        /// </summary>
        [Authorize]
        [HttpPut("tag")]
        public async Task<IActionResult> UpdateUserTag([FromBody] UpdateUserTagDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Validate that the tag is one of the user's earned medals
            if (!string.IsNullOrEmpty(dto.Tag))
            {
                var hasMedal = await _context.UtilizadorMedalhas
                    .Include(um => um.Medalha)
                    .AnyAsync(um => um.UtilizadorId == userId && um.Medalha.Nome == dto.Tag);
                
                if (!hasMedal)
                    return BadRequest(new { message = "Tag inválida. Deve ser o nome de uma medalha conquistada." });
            }

            user.UserTag = dto.Tag;
            user.UserTagPrimaryColor = dto.PrimaryColor;
            user.UserTagSecondaryColor = dto.SecondaryColor;
            await _context.SaveChangesAsync();
            return Ok(new { tag = user.UserTag, primaryColor = user.UserTagPrimaryColor, secondaryColor = user.UserTagSecondaryColor });
        }


        // DTOs
        /// <summary>
        /// Molde robusto usado para aceitar o Payload formatado do Formulário de Edição (Angular).
        /// </summary>
        public class UpdateProfileDto
        {
            public string? UserName { get; set; }
            public string? Bio { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("fotoPerfilUrl")]
            public string? FotoPerfilUrl { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("capaUrl")]
            public string? CapaUrl { get; set; }
        }

        /// <summary>
        /// Recetáculo contendo as int Keys dos genêros (TMDb) selecionados nas caixas do modal.
        /// </summary>
        public class AtualizarGenerosFavoritosDto
        {
            public List<int>? GeneroIds { get; set; }
        }

        /// <summary>
        /// Payload com submissão hexadécima ou nome cru vindo das opções CSS da Tag em destaque.
        /// </summary>
        public class UpdateUserTagDto
        {
            public string? Tag { get; set; }
            public string? PrimaryColor { get; set; }
            public string? SecondaryColor { get; set; }
        }
    }
}
