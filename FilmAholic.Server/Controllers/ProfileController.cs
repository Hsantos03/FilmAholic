using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;



namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;

        public ProfileController(FilmAholicDbContext context)
        {
            _context = context;
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
                    bio = u.Bio
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

        // DTO used to receive profile updates
        public class UpdateProfileDto
        {
            // JSON binding is case-insensitive; client may send "userName" or "UserName".
            public string? UserName { get; set; }
            public string? Bio { get; set; }
        }
    }
}
