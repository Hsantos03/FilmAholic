using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
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

        private static string? IconUrlFromFileName(string? fileName, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            return $"{baseUrl}/uploads/comunidades/icons/{fileName}";
        }

        // ─── Helper: guardar imagem num directório ────
        private async Task<string?> SaveImageAsync(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(uploadsRoot))
                uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var targetDir = Path.Combine(uploadsRoot, "uploads", subFolder);
            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

            var ext = Path.GetExtension(file.FileName);
            var safeName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(targetDir, safeName);

            await using (var stream = System.IO.File.Create(filePath))
                await file.CopyToAsync(stream);

            return safeName;
        }

        // ─── Helper: apagar ficheiro de imagem ────
        private void DeleteImageFile(string? fileName, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            var uploadsRoot = _env.WebRootPath;
            if (string.IsNullOrEmpty(uploadsRoot))
                uploadsRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var filePath = Path.Combine(uploadsRoot, "uploads", subFolder, fileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        // ─── Public list ────
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
                    c.IconFileName,
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
                BannerUrl = BannerUrlFromFileName(x.BannerFileName, baseUrl),
                IconUrl = IconUrlFromFileName(x.IconFileName, baseUrl)
            }).ToList();

            return Ok(list);
        }

        // ─── Public detail ─────
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
                BannerUrl = BannerUrlFromFileName(c.BannerFileName, baseUrl),
                IconUrl = IconUrlFromFileName(c.IconFileName, baseUrl)
            };

            return Ok(dto);
        }

        // ─── Create (requires authenticated user) ─────
        [Authorize]
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Create([FromForm] ComunidadeCreateForm form)
        {
            if (string.IsNullOrWhiteSpace(form.Nome))
                return BadRequest(new { message = "Nome obrigatório." });

            try
            {
                var exists = await _context.Comunidades.AnyAsync(c => c.Nome.ToLower() == form.Nome.Trim().ToLower());
                if (exists) return Conflict(new { message = "Já existe uma comunidade com esse nome." });

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

                var bannerFileName = await SaveImageAsync(form.Banner, "comunidades");
                var iconFileName = await SaveImageAsync(form.Icon, "comunidades/icons");

                var entity = new Comunidade
                {
                    Nome = form.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim(),
                    BannerFileName = bannerFileName,
                    IconFileName = iconFileName,
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
                    BannerUrl = BannerUrlFromFileName(entity.BannerFileName, baseUrl),
                    IconUrl = IconUrlFromFileName(entity.IconFileName, baseUrl)
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

        // ─── UPDATE (apenas o Admin/criador da comunidade) ────
        [Authorize]
        [HttpPut("{id:int}")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Update(int id, [FromForm] ComunidadeUpdateForm form)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin");

            if (!isAdmin) return Forbid();

            var comunidade = await _context.Comunidades.FirstOrDefaultAsync(c => c.Id == id);
            if (comunidade == null) return NotFound();

            if (string.IsNullOrWhiteSpace(form.Nome))
                return BadRequest(new { message = "O nome é obrigatório." });

            var nomeEmUso = await _context.Comunidades
                .AnyAsync(c => c.Id != id && c.Nome.ToLower() == form.Nome.Trim().ToLower());
            if (nomeEmUso)
                return Conflict(new { message = "Já existe outra comunidade com esse nome." });

            comunidade.Nome = form.Nome.Trim();
            comunidade.Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim();

            if (form.Banner != null && form.Banner.Length > 0)
            {
                DeleteImageFile(comunidade.BannerFileName, "comunidades");
                comunidade.BannerFileName = await SaveImageAsync(form.Banner, "comunidades");
            }

            if (form.Icon != null && form.Icon.Length > 0)
            {
                DeleteImageFile(comunidade.IconFileName, "comunidades/icons");
                comunidade.IconFileName = await SaveImageAsync(form.Icon, "comunidades/icons");
            }

            await _context.SaveChangesAsync();

            var baseUrl = PublicBaseUrl();
            var dto = new ComunidadeDto
            {
                Id = comunidade.Id,
                Nome = comunidade.Nome,
                Descricao = comunidade.Descricao,
                DataCriacao = comunidade.DataCriacao,
                MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == comunidade.Id),
                BannerUrl = BannerUrlFromFileName(comunidade.BannerFileName, baseUrl),
                IconUrl = IconUrlFromFileName(comunidade.IconFileName, baseUrl)
            };

            return Ok(dto);
        }

        // ─── DELETE apagar comunidade (apenas o Admin/criador) ────
        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin");

            if (!isAdmin) return Forbid();

            var comunidade = await _context.Comunidades.FirstOrDefaultAsync(c => c.Id == id);
            if (comunidade == null) return NotFound();

            DeleteImageFile(comunidade.BannerFileName, "comunidades");
            DeleteImageFile(comunidade.IconFileName, "comunidades/icons");

            _context.Comunidades.Remove(comunidade);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Comunidade apagada com sucesso." });
        }

        // ─── GET membros da comunidade ──────
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
                    DataEntrada = m.DataEntrada,
                    CastigadoAte = m.CastigadoAte
                })
                .ToListAsync();

            return Ok(membros);
        }

        // ─── GET posts da comunidade ──────
        [HttpGet("{id:int}/posts")]
        public async Task<IActionResult> GetPosts(int id)
        {
            var baseUrl = PublicBaseUrl();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

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
                    AutorId = p.UtilizadorId,
                    ImagemUrl = p.ImagemUrl != null ? baseUrl + p.ImagemUrl : null,
                    AutorNome = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == p.UtilizadorId)
                        .Select(u => u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    
                    LikesCount = _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && v.IsLike),
                    DislikesCount = _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && !v.IsLike),
                    
                    ReportsCount = _context.ComunidadePostReports.Count(r => r.PostId == p.Id),
                    
                    UserVote = currentUserId == null ? 0 : 
                        _context.ComunidadePostVotos
                        .Where(v => v.PostId == p.Id && v.UtilizadorId == currentUserId)
                        .Select(v => v.IsLike ? 1 : -1)
                        .FirstOrDefault(),
                    
                    TemSpoiler = p.TemSpoiler
                })
                .ToListAsync();

            return Ok(posts);
        }

        // ─── POST criar publicação (apenas membros) ────
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

            var membroInfo = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (membroInfo?.CastigadoAte != null && membroInfo.CastigadoAte > DateTime.UtcNow)
            {
                return BadRequest(new { message = "Estás castigado e não podes publicar no momento." });
            }

            var imagemFileName = await SaveImageAsync(form.Imagem, "posts");

            var post = new ComunidadePost
            {
                ComunidadeId = id,
                UtilizadorId = userId,
                Titulo = form.Titulo.Trim(),
                Conteudo = form.Conteudo.Trim(),
                ImagemUrl = imagemFileName != null ? $"/uploads/posts/{imagemFileName}" : null,
                DataCriacao = DateTime.UtcNow,
                DataAtualizacao = DateTime.UtcNow,
                TemSpoiler = form.TemSpoiler
            };

            _context.ComunidadePosts.Add(post);
            await _context.SaveChangesAsync();

            try
            {
                var autor = await _context.Users
                    .OfType<Utilizador>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var baseUrl = PublicBaseUrl();
                
                var dtoCompleto = new PostDto
                {
                    Id = post.Id,
                    Titulo = post.Titulo,
                    Conteudo = post.Conteudo,
                    DataCriacao = post.DataCriacao,
                    AutorId = userId, 
                    AutorNome = autor != null ? (autor.Nome + " " + autor.Sobrenome).Trim() : "Desconhecido",
                    ImagemUrl = post.ImagemUrl != null ? $"{baseUrl}{post.ImagemUrl}" : null,
                    
                    LikesCount = 0,
                    DislikesCount = 0,
                    UserVote = 0,
                    ReportsCount = 0,
                    TemSpoiler = post.TemSpoiler
                };

                return Ok(dtoCompleto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Erro ao criar post." });
            }
        }

        // ─── POST juntar-se à comunidade ────
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

        // ─── DELETE sair da comunidade ────
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

        // ─── DELETE remover membro e o seu histórico (apenas Admin) ────
        [Authorize]
        [HttpDelete("{id:int}/membros/{utilizadorId}")]
        public async Task<IActionResult> RemoverMembro(int id, string utilizadorId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == currentUserId && m.Role == "Admin");

            if (!isAdmin) return Forbid();

            if (currentUserId == utilizadorId)
                return BadRequest(new { message = "Não podes remover-te a ti próprio." });

            var membroARemover = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == utilizadorId);

            if (membroARemover == null) return NotFound(new { message = "Membro não encontrado." });

            var votosNaComunidade = await _context.ComunidadePostVotos
                .Include(v => v.Post)
                .Where(v => v.UtilizadorId == utilizadorId && v.Post.ComunidadeId == id)
                .ToListAsync();
            _context.ComunidadePostVotos.RemoveRange(votosNaComunidade);

            var reportsNaComunidade = await _context.ComunidadePostReports
                .Include(r => r.Post)
                .Where(r => r.UtilizadorId == utilizadorId && r.Post.ComunidadeId == id)
                .ToListAsync();
            _context.ComunidadePostReports.RemoveRange(reportsNaComunidade);

            var postsDoUtilizador = await _context.ComunidadePosts
                .Where(p => p.UtilizadorId == utilizadorId && p.ComunidadeId == id)
                .ToListAsync();
            _context.ComunidadePosts.RemoveRange(postsDoUtilizador);

            _context.ComunidadeMembros.Remove(membroARemover);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Membro e respetivo histórico removidos com sucesso." });
        }

        // ─── POST Aplicar Castigo (Admin) ────
        [Authorize]
        [HttpPost("{id:int}/membros/{utilizadorId}/castigar")]
        public async Task<IActionResult> CastigarMembro(int id, string utilizadorId, [FromQuery] int horas)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == currentUserId && m.Role == "Admin");

            if (!isAdmin) return Forbid();

            if (currentUserId == utilizadorId)
                return BadRequest(new { message = "Não te podes castigar a ti próprio." });

            var membro = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == utilizadorId);

            if (membro == null) return NotFound(new { message = "Membro não encontrado." });

            membro.CastigadoAte = DateTime.UtcNow.AddHours(horas);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Membro castigado com sucesso.", castigadoAte = membro.CastigadoAte });
        }

        [Authorize]
        [HttpGet("sugestoes-filmes")]
        public async Task<IActionResult> GetSugestoesFilmesComunidade([FromQuery] int limit = 24)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (limit < 1) limit = 1;
            if (limit > 60) limit = 60;

            var minhasComunidades = await _context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.UtilizadorId == userId && m.Status == "Ativo")
                .Select(m => m.ComunidadeId)
                .Distinct()
                .ToListAsync();

            if (minhasComunidades.Count == 0)
                return Ok(Array.Empty<SugestaoFilmeComunidadeDto>());

            var filmesJaVistos = await _context.UserMovies
                .AsNoTracking()
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync();

            var acumulado = new List<(int FilmeId, int ComunidadeId, string ComunidadeNome, int MembrosQueViram)>();

            foreach (var cid in minhasComunidades)
            {
                var comunidadeNome = await _context.Comunidades
                    .AsNoTracking()
                    .Where(c => c.Id == cid)
                    .Select(c => c.Nome)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(comunidadeNome))
                    continue;

                var outrosMembros = await _context.ComunidadeMembros
                    .AsNoTracking()
                    .Where(m => m.ComunidadeId == cid && m.UtilizadorId != userId && m.Status == "Ativo")
                    .Select(m => m.UtilizadorId)
                    .ToListAsync();

                if (outrosMembros.Count == 0)
                    continue;

                var grupos = await _context.UserMovies
                    .AsNoTracking()
                    .Where(um => um.JaViu && outrosMembros.Contains(um.UtilizadorId))
                    .Where(um => !filmesJaVistos.Contains(um.FilmeId))
                    .GroupBy(um => um.FilmeId)
                    .Select(g => new { FilmeId = g.Key, Cnt = g.Select(x => x.UtilizadorId).Distinct().Count() })
                    .OrderByDescending(x => x.Cnt)
                    .Take(12)
                    .ToListAsync();

                foreach (var g in grupos)
                    acumulado.Add((g.FilmeId, cid, comunidadeNome, g.Cnt));
            }

            var ordenado = acumulado
                .OrderByDescending(x => x.MembrosQueViram)
                .ThenBy(x => x.ComunidadeNome)
                .Take(limit)
                .ToList();

            var idsFilmes = ordenado.Select(x => x.FilmeId).Distinct().ToList();
            var filmes = await _context.Filmes
                .AsNoTracking()
                .Where(f => idsFilmes.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id);

            var resultado = new List<SugestaoFilmeComunidadeDto>();
            foreach (var row in ordenado)
            {
                if (!filmes.TryGetValue(row.FilmeId, out var f))
                    continue;

                resultado.Add(new SugestaoFilmeComunidadeDto
                {
                    FilmeId = f.Id,
                    Titulo = f.Titulo,
                    Genero = f.Genero,
                    PosterUrl = f.PosterUrl,
                    Duracao = f.Duracao,
                    Ano = f.Ano,
                    ReleaseDate = f.ReleaseDate,
                    ComunidadeId = row.ComunidadeId,
                    ComunidadeNome = row.ComunidadeNome,
                    MembrosQueViram = row.MembrosQueViram
                });
            }

            return Ok(resultado);
        }

        // ─── POST Votar num Post (Like / Dislike) ────
        [Authorize]
        [HttpPost("{id:int}/posts/{postId:int}/votar")]
        public async Task<IActionResult> VotarPost(int id, int postId, [FromQuery] bool isLike)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var votoExistente = await _context.ComunidadePostVotos
                .FirstOrDefaultAsync(v => v.PostId == postId && v.UtilizadorId == userId);

            if (votoExistente != null)
            {
                if (votoExistente.IsLike == isLike)
                {
                    _context.ComunidadePostVotos.Remove(votoExistente);
                }
                else
                {
                    votoExistente.IsLike = isLike;
                }
            }
            else
            {
                _context.ComunidadePostVotos.Add(new ComunidadePostVoto
                {
                    PostId = postId,
                    UtilizadorId = userId,
                    IsLike = isLike
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── PUT Editar Post (Só o dono) ────
        [Authorize]
        [HttpPut("{id:int}/posts/{postId:int}")]
        public async Task<IActionResult> EditPost(int id, int postId, [FromBody] PostEditDto form)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var post = await _context.ComunidadePosts.FirstOrDefaultAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (post == null) return NotFound();

            if (post.UtilizadorId != userId) return Forbid();

            if (string.IsNullOrWhiteSpace(form.Titulo) || string.IsNullOrWhiteSpace(form.Conteudo))
                return BadRequest(new { message = "Título e conteúdo são obrigatórios." });

            post.Titulo = form.Titulo.Trim();
            post.Conteudo = form.Conteudo.Trim();
            post.TemSpoiler = form.TemSpoiler;
            post.DataAtualizacao = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ─── DELETE Apagar Post (Dono do post OU Admin da comunidade) ────
        [Authorize]
        [HttpDelete("{id:int}/posts/{postId:int}")]
        public async Task<IActionResult> DeletePost(int id, int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var post = await _context.ComunidadePosts.FirstOrDefaultAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (post == null) return NotFound();

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin");

            if (post.UtilizadorId != userId && !isAdmin) return Forbid();

            _context.ComunidadePosts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Publicação apagada com sucesso." });
        }

        // ─── POST Reportar Post ────
        [Authorize]
        [HttpPost("{id:int}/posts/{postId:int}/report")]
        public async Task<IActionResult> ReportPost(int id, int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var post = await _context.ComunidadePosts.FirstOrDefaultAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (post == null) return NotFound();

            var jaReportou = await _context.ComunidadePostReports
                .AnyAsync(r => r.PostId == postId && r.UtilizadorId == userId);

            if (!jaReportou)
            {
                _context.ComunidadePostReports.Add(new ComunidadePostReport
                {
                    PostId = postId,
                    UtilizadorId = userId
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Publicação reportada com sucesso." });
        }

        // ─── DTOs & Forms ─────────────────────────────────────────────────────

        public class PostDto
        {
            public int Id { get; set; }
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public DateTime DataCriacao { get; set; }
            public string? AutorId { get; set; } 
            public string? AutorNome { get; set; }
            public string? ImagemUrl { get; set; }
            public int LikesCount { get; set; } 
            public int DislikesCount { get; set; } 
            public int UserVote { get; set; } 
            public int ReportsCount { get; set; } 
            public bool TemSpoiler { get; set; } 
        }

        public class MembroDto
        {
            public string? UtilizadorId { get; set; }
            public string? UserName { get; set; }
            public string? Role { get; set; }
            public DateTime DataEntrada { get; set; }
            public DateTime? CastigadoAte { get; set; } 
        }

        public class ComunidadeDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = "";
            public string? Descricao { get; set; }
            public DateTime DataCriacao { get; set; }
            public int MembrosCount { get; set; }
            public string? BannerUrl { get; set; }
            public string? IconUrl { get; set; }
        }

        public class PostCreateForm
        {
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public IFormFile? Imagem { get; set; }
            [FromForm(Name = "temSpoiler")]
            public bool TemSpoiler { get; set; } 
        }

        public class ComunidadeCreateForm
        {
            [FromForm(Name = "nome")]
            public string Nome { get; set; } = "";

            [FromForm(Name = "descricao")]
            public string? Descricao { get; set; }

            [FromForm(Name = "banner")]
            public IFormFile? Banner { get; set; }

            [FromForm(Name = "icon")]
            public IFormFile? Icon { get; set; }
        }

        public class ComunidadeUpdateForm
        {
            [FromForm(Name = "nome")]
            public string Nome { get; set; } = "";

            [FromForm(Name = "descricao")]
            public string? Descricao { get; set; }

            [FromForm(Name = "banner")]
            public IFormFile? Banner { get; set; }

            [FromForm(Name = "icon")]
            public IFormFile? Icon { get; set; }
        }

        public class PostEditDto
        {
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public bool TemSpoiler { get; set; } 
        }
    }
}