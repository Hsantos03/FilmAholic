using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using FilmAholic.Server.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;
        private readonly IPreferenciasService _preferenciasService;
        private readonly UserManager<Utilizador> _userManager;

        public ProfileController(FilmAholicDbContext context, IPreferenciasService preferenciasService, UserManager<Utilizador> userManager)
        {
            _context = context;
            _preferenciasService = preferenciasService;
            _userManager = userManager;
        }

        // GET: api/Profile/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var user = await _context.Users
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    id = u.Id,
                    userName = u.UserName,
                    nome = u.Nome,
                    sobrenome = u.Sobrenome,
                    email = u.Email,
                    fotoPerfilUrl = u.FotoPerfilUrl,
                    dataCriacao = u.DataCriacao,
                    bio = u.Bio,
                    generosFavoritos = u.GenerosFavoritos.Select(ug => new
                    {
                        id = ug.Genero.Id,
                        nome = ug.Genero.Nome
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // PUT: api/Profile/{id}
        // Accepts JSON with optional fields: userName, bio
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(string id, [FromBody] UpdateProfileDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound();

            // Update username (and normalized username) if provided and different
            if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
            {
                user.UserName = dto.UserName;
                user.NormalizedUserName = dto.UserName.ToUpperInvariant();
            }

            // Update bio (allow empty string to clear it)
            if (dto.Bio is not null)
            {
                user.Bio = dto.Bio;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // handle unique index or other DB errors
                return StatusCode(500, new { message = "Failed to update profile.", detail = ex.Message });
            }

            return NoContent();
        }

        // DELETE: api/Profile/{id}
        // Only allow the authenticated user to delete their own account
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id is required." });

            var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(callerId) || callerId != id)
            {
                return Forbid();
            }

            var user = await _context.Users
                .Include(u => u.GenerosFavoritos)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            try
            {
                // Remove linked user data (UserMovies, UtilizadorGeneros) explicitly to be safe
                var userMovies = _context.UserMovies.Where(um => um.UtilizadorId == id);
                _context.UserMovies.RemoveRange(userMovies);

                var utilGen = _context.UtilizadorGeneros.Where(ug => ug.UtilizadorId == id);
                _context.UtilizadorGeneros.RemoveRange(utilGen);

                // Use UserManager to delete the identity user (this will clean up identity tables)
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { message = "Failed to delete user.", errors = result.Errors });
                }

                // Save remaining changes (if any)
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting account.", detail = ex.Message });
            }
        }

        // GET: api/Profile/generos
        // Obter todos os géneros disponíveis
        [HttpGet("generos")]
        public async Task<IActionResult> ObterTodosGeneros()
        {
            var generos = await _preferenciasService.ObterTodosGenerosAsync();
            return Ok(generos.Select(g => new { id = g.Id, nome = g.Nome }));
        }

        // GET: api/Profile/{id}/generos-favoritos
        // Obter géneros favoritos de um utilizador
        [HttpGet("{id}/generos-favoritos")]
        public async Task<IActionResult> ObterGenerosFavoritos(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id é obrigatório." });

            var generos = await _preferenciasService.ObterGenerosFavoritosAsync(id);
            return Ok(generos.Select(g => new { id = g.Id, nome = g.Nome }));
        }

        // PUT: api/Profile/{id}/generos-favoritos
        // Atualizar géneros favoritos de um utilizador
        [HttpPut("{id}/generos-favoritos")]
        public async Task<IActionResult> AtualizarGenerosFavoritos(string id, [FromBody] AtualizarGenerosFavoritosDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new { message = "Id é obrigatório." });

            // Verificar se o utilizador existe
            var utilizadorExiste = await _context.Users.AnyAsync(u => u.Id == id);
            if (!utilizadorExiste)
                return NotFound(new { message = "Utilizador não encontrado." });

            // Validar géneros
            if (dto.GeneroIds != null && dto.GeneroIds.Any())
            {
                foreach (var generoId in dto.GeneroIds)
                {
                    if (!await _preferenciasService.GeneroExisteAsync(generoId))
                    {
                        return BadRequest(new { message = $"Género com ID {generoId} não encontrado." });
                    }
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

        [Authorize]
        [HttpPut("favorites")]
        public async Task<IActionResult> UpdateFavorites([FromBody] FavoritosDTO dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return NotFound();

            user.TopFilmes = JsonSerializer.Serialize(dto.Filmes.Take(10));
            user.TopAtores = JsonSerializer.Serialize(dto.Atores.Take(10));

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DTO used to receive profile updates
        public class UpdateProfileDto
        {
            // JSON binding is case-insensitive; client may send "userName" or "UserName".
            public string? UserName { get; set; }
            public string? Bio { get; set; }
        }

        // DTO used to receive genre preferences updates
        public class AtualizarGenerosFavoritosDto
        {
            public List<int>? GeneroIds { get; set; }
        }
    }
}
