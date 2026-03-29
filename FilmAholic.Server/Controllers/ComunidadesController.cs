using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComunidadesController : ControllerBase
    {
        private readonly FilmAholicDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ComunidadesController> _logger;

        public ComunidadesController(FilmAholicDbContext context, IWebHostEnvironment env, ILogger<ComunidadesController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // Public list
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.Comunidades
                .AsNoTracking()
                .Select(c => new ComunidadeDto
                {
                    Id = c.Id,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    DataCriacao = c.DataCriacao,
                    MembrosCount = _context.ComunidadeMembros.Count(m => m.ComunidadeId == c.Id),
                    BannerUrl = null // will be filled in below
                })
                .ToListAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
            foreach (var dto in list)
            {
                // try to resolve banner by checking a stored banner filename (if you later persist filename in the model)
                // currently we return null (frontend handles missing)
            }

            return Ok(list);
        }

        // Public detail
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _context.Comunidades
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();

            var dto = new ComunidadeDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Descricao = c.Descricao,
                DataCriacao = c.DataCriacao,
                MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == c.Id),
                BannerUrl = null
            };

            // If you later add a BannerFileName property to the model, construct absolute URL here.
            return Ok(dto);
        }

        // Create (requires authenticated user)
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(10_000_000)] // allow up to ~10MB, tune as needed
        public async Task<IActionResult> Create([FromForm] ComunidadeCreateForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            try
            {
                // enforce unique name (case-insensitive)
                var exists = await _context.Comunidades.AnyAsync(c => c.Nome.ToLower() == form.Nome.Trim().ToLower());
                if (exists) return Conflict(new { message = "Já existe uma comunidade com esse nome." });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                var entity = new Comunidade
                {
                    Nome = form.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim(),
                    CreatedById = userId,
                    DataCriacao = DateTime.UtcNow
                };

                // handle banner file upload if provided
                if (form.Banner != null && form.Banner.Length > 0)
                {
                    var uploadsRoot = _env.WebRootPath;
                    if (string.IsNullOrEmpty(uploadsRoot))
                    {
                        // fallback to wwwroot in content root
                        uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    }

                    var targetDir = Path.Combine(uploadsRoot, "uploads", "comunidades");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    var ext = Path.GetExtension(form.Banner.FileName);
                    var safeName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(targetDir, safeName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await form.Banner.CopyToAsync(stream);
                    }

                    // store banner URL in a transient way: set a field on the model if you add it later.
                    // For now we will return the URL in the DTO after save.
                    var baseUrl = $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');
                    var bannerUrl = $"{baseUrl}/uploads/comunidades/{safeName}";

                    // Persist the banner URL somewhere: easiest is to extend Comunidade with BannerUrl property later.
                    // For now we will return it in the DTO by keeping it in a local variable.
                    await _context.Comunidades.AddAsync(entity);
                    await _context.SaveChangesAsync();

                    // create member record for creator
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var member = new ComunidadeMembro
                        {
                            ComunidadeId = entity.Id,
                            UtilizadorId = userId,
                            Role = "Admin",
                            Status = "Ativo",
                            DataEntrada = DateTime.UtcNow
                        };
                        _context.ComunidadeMembros.Add(member);
                        await _context.SaveChangesAsync();
                    }

                    var dtoWithBanner = new ComunidadeDto
                    {
                        Id = entity.Id,
                        Nome = entity.Nome,
                        Descricao = entity.Descricao,
                        DataCriacao = entity.DataCriacao,
                        MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == entity.Id),
                        BannerUrl = bannerUrl
                    };

                    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dtoWithBanner);
                }
                else
                {
                    await _context.Comunidades.AddAsync(entity);
                    await _context.SaveChangesAsync();

                    if (!string.IsNullOrEmpty(userId))
                    {
                        var member = new ComunidadeMembro
                        {
                            ComunidadeId = entity.Id,
                            UtilizadorId = userId,
                            Role = "Admin",
                            Status = "Ativo",
                            DataEntrada = DateTime.UtcNow
                        };
                        _context.ComunidadeMembros.Add(member);
                        await _context.SaveChangesAsync();
                    }

                    var dto = new ComunidadeDto
                    {
                        Id = entity.Id,
                        Nome = entity.Nome,
                        Descricao = entity.Descricao,
                        DataCriacao = entity.DataCriacao,
                        MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == entity.Id),
                        BannerUrl = null
                    };

                    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DB error creating comunidade");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro ao criar comunidade (BD)." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comunidade");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro ao criar comunidade." });
            }
        }

        // Simple DTOs / Form binding classes
        public class ComunidadeDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = "";
            public string? Descricao { get; set; }
            public DateTime DataCriacao { get; set; }
            public int MembrosCount { get; set; }
            public string? BannerUrl { get; set; }
        }

        public class ComunidadeCreateForm
        {
            [FromForm(Name = "nome")]
            public string Nome { get; set; } = "";

            [FromForm(Name = "descricao")]
            public string? Descricao { get; set; }

            [FromForm(Name = "banner")]
            public IFormFile? Banner { get; set; }
        }
    }
}