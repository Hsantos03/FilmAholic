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

        private string PublicBaseUrl() => $"{Request.Scheme}://{Request.Host.Value}".TrimEnd('/');

        private static string? BannerUrlFromFileName(string? fileName, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return $"{baseUrl}/uploads/comunidades/{fileName}";
        }

        // Public list
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var baseUrl = PublicBaseUrl();
            var rows = await _context.Comunidades
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Descricao,
                    c.DataCriacao,
                    c.BannerFileName,
                    MembrosCount = _context.ComunidadeMembros.Count(m => m.ComunidadeId == c.Id)
                })
                .ToListAsync();

            var list = rows.Select(x => new ComunidadeDto
            {
                Id = x.Id,
                Nome = x.Nome,
                Descricao = x.Descricao,
                DataCriacao = x.DataCriacao,
                MembrosCount = x.MembrosCount,
                BannerUrl = BannerUrlFromFileName(x.BannerFileName, baseUrl)
            }).ToList();

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

            var baseUrl = PublicBaseUrl();
            var dto = new ComunidadeDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Descricao = c.Descricao,
                DataCriacao = c.DataCriacao,
                MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == c.Id),
                BannerUrl = BannerUrlFromFileName(c.BannerFileName, baseUrl)
            };

            return Ok(dto);
        }

        // Create (requires authenticated user)
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(10_000_000)] // até ~10MB
        public async Task<IActionResult> Create([FromForm] ComunidadeCreateForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nome))
                return BadRequest(new { message = "Nome   obrigat rio." });

            try
            {
                var exists = await _context.Comunidades.AnyAsync(c => c.Nome.ToLower() == form.Nome.Trim().ToLower());
                if (exists) return Conflict(new { message = "J  existe uma comunidade com esse nome." });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

                string? bannerFileName = null;
                if (form.Banner != null && form.Banner.Length > 0)
                {
                    var uploadsRoot = _env.WebRootPath;
                    if (string.IsNullOrEmpty(uploadsRoot))
                        uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                    var targetDir = Path.Combine(uploadsRoot, "uploads", "comunidades");
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    var ext = Path.GetExtension(form.Banner.FileName);
                    var safeName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(targetDir, safeName);

                    await using (var stream = System.IO.File.Create(filePath))
                    {
                        await form.Banner.CopyToAsync(stream);
                    }

                    bannerFileName = safeName;
                }

                var entity = new Comunidade
                {
                    Nome = form.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim(),
                    BannerFileName = bannerFileName,
                    CreatedById = userId,
                    DataCriacao = DateTime.UtcNow
                };

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

                var baseUrl = PublicBaseUrl();
                var dto = new ComunidadeDto
                {
                    Id = entity.Id,
                    Nome = entity.Nome,
                    Descricao = entity.Descricao,
                    DataCriacao = entity.DataCriacao,
                    MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == entity.Id),
                    BannerUrl = BannerUrlFromFileName(entity.BannerFileName, baseUrl)
                };

                return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
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

        // GET membros da comunidade
        [HttpGet("{id:int}/membros")]
        public async Task<IActionResult> GetMembros(int id)
        {
            var membros = await _context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.ComunidadeId == id)
                .Select(m => new MembroDto
                {
                    UtilizadorId = m.UtilizadorId,
                    UserName = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == m.UtilizadorId)
                        .Select(u => u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    Role = m.Role,
                    DataEntrada = m.DataEntrada
                })
                .ToListAsync();

            return Ok(membros);
        }

        // GET posts da comunidade
        [HttpGet("{id:int}/posts")]
        public async Task<IActionResult> GetPosts(int id)
        {
            var baseUrl = PublicBaseUrl();
            var posts = await _context.ComunidadePosts
                .AsNoTracking()
                .Where(p => p.ComunidadeId == id)
                .OrderByDescending(p => p.DataCriacao)
                .Select(p => new PostDto
                {
                    Id = p.Id,
                    Titulo = p.Titulo,
                    Conteudo = p.Conteudo,
                    DataCriacao = p.DataCriacao,
                    ImagemUrl = p.ImagemUrl != null ? baseUrl + p.ImagemUrl : null,
                    AutorNome = _context.Users
                        .OfType<Utilizador>()
                        .Where(u => u.Id == p.UtilizadorId)
                        .Select(u => u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido"
                })
                .ToListAsync();

            return Ok(posts);
        }

        // POST criar publicação (apenas membros)
        [Authorize]
        [HttpPost("{id:int}/posts")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> CreatePost(int id, [FromForm] PostCreateForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Titulo) || string.IsNullOrWhiteSpace(form.Conteudo))
                return BadRequest(new { message = "Título e conteúdo são obrigatórios." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var isMembro = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (!isMembro) return Forbid();

            // Guardar imagem se existir
            string? imagemFileName = null;
            if (form.Imagem != null && form.Imagem.Length > 0)
            {
                var uploadsRoot = _env.WebRootPath;
                if (string.IsNullOrEmpty(uploadsRoot))
                    uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                var targetDir = Path.Combine(uploadsRoot, "uploads", "posts");
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                var ext = Path.GetExtension(form.Imagem.FileName);
                var safeName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(targetDir, safeName);

                await using (var stream = System.IO.File.Create(filePath))
                {
                    await form.Imagem.CopyToAsync(stream);
                }

                imagemFileName = safeName;
            }

            var post = new ComunidadePost
            {
                ComunidadeId = id,
                UtilizadorId = userId,
                Titulo = form.Titulo.Trim(),
                Conteudo = form.Conteudo.Trim(),
                ImagemUrl = imagemFileName != null ? $"/uploads/posts/{imagemFileName}" : null,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow
            };

            _context.ComunidadePosts.Add(post);
            await _context.SaveChangesAsync();

            var autor = await _context.Users
                .OfType<Utilizador>()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var baseUrl = PublicBaseUrl();
            return Ok(new PostDto
            {
                Id = post.Id,
                Titulo = post.Titulo,
                Conteudo = post.Conteudo,
                DataCriacao = post.DataCriacao,
                AutorNome = autor != null ? (autor.Nome + " " + autor.Sobrenome).Trim() : "Desconhecido",
                ImagemUrl = post.ImagemUrl != null ? $"{baseUrl}{post.ImagemUrl}" : null
            });
        }

        // POST juntar-se à comunidade
        [Authorize]
        [HttpPost("{id:int}/juntar")]
        public async Task<IActionResult> Juntar(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var jaExiste = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (jaExiste) return Conflict(new { message = "Já és membro desta comunidade." });

            _context.ComunidadeMembros.Add(new ComunidadeMembro
            {
                ComunidadeId = id,
                UtilizadorId = userId!,
                Role = "Membro",
                Status = "Ativo",
                DataEntrada = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE sair da comunidade
        [Authorize]
        [HttpDelete("{id:int}/sair")]
        public async Task<IActionResult> Sair(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var membro = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (membro == null) return NotFound();

            _context.ComunidadeMembros.Remove(membro);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // DTOs
        public class MembroDto
        {
            public string? UtilizadorId { get; set; }
            public string? UserName { get; set; }
            public string? Role { get; set; }
            public DateTime DataEntrada { get; set; }
        }

        public class PostDto
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public DateTime DataCriacao { get; set; }
            public string? AutorNome { get; set; }
            public string? ImagemUrl { get; set; }
        }

        public class PostCreateForm
        {
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public IFormFile? Imagem { get; set; }
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