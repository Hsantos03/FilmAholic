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

        private async Task<bool> IsLimiteAtingidoAsync(int comunidadeId, int? limiteMembros)
        {
            if (!limiteMembros.HasValue || limiteMembros.Value <= 0) return false;
            var membrosAtivos = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == comunidadeId && m.Status == "Ativo");
            return membrosAtivos >= limiteMembros.Value;
        }

        private static bool MembroCastigadoAtivo(ComunidadeMembro? m, DateTime agoraUtc) =>
            m?.CastigadoAte != null && m.CastigadoAte > agoraUtc;

        private static bool BanimentoAtivo(ComunidadeMembro? m, DateTime agoraUtc) =>
            m != null && m.Status == "Banido" && (m.BanidoAte == null || m.BanidoAte > agoraUtc);

        private async Task<bool> UtilizadorBanidoAtivoNaComunidadeAsync(int comunidadeId, string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            var agora = DateTime.UtcNow;
            return await _context.ComunidadeMembros.AsNoTracking()
                .AnyAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == userId && m.Status == "Banido"
                    && (m.BanidoAte == null || m.BanidoAte > agora));
        }

        private async Task<IActionResult?> ForbidSeBanidoAsync(int comunidadeId, string? userId)
        {
            if (!await UtilizadorBanidoAtivoNaComunidadeAsync(comunidadeId, userId)) return null;
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Foste banido desta comunidade." });
        }

        private static string? BuildKickCorpo(string? motivo) =>
            string.IsNullOrWhiteSpace(motivo) ? null : $"Motivo: {motivo.Trim()}";

        private static string BuildBanCorpo(DateTime? banidoAteUtc, string? motivo)
        {
            var duracao = banidoAteUtc == null
                ? "Duração: permanente."
                : $"Duração: até {banidoAteUtc.Value:dd/MM/yyyy HH:mm} (UTC).";
            if (string.IsNullOrWhiteSpace(motivo))
                return duracao;
            return $"{duracao} Motivo: {motivo.Trim()}";
        }

        // ─── Public list ────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var baseUrl = PublicBaseUrl();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var agora = DateTime.UtcNow;
            var rows = await _context.Comunidades
                .AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.Nome,
                    c.Descricao,
                    c.LimiteMembros,
                    c.IsPrivada,
                    c.DataCriacao,
                    c.BannerFileName,
                    c.IconFileName,
                    MembrosCount = _context.ComunidadeMembros.Count(m => m.ComunidadeId == c.Id && m.Status == "Ativo"),
                    IsCurrentUserBanned = !string.IsNullOrWhiteSpace(userId) && _context.ComunidadeMembros.Any(m =>
                        m.ComunidadeId == c.Id && m.UtilizadorId == userId && m.Status == "Banido"
                        && (m.BanidoAte == null || m.BanidoAte > agora)),
                    MeuBanimentoAteUtc = string.IsNullOrWhiteSpace(userId) ? null :
                        _context.ComunidadeMembros
                            .Where(m => m.ComunidadeId == c.Id && m.UtilizadorId == userId && m.Status == "Banido"
                                && (m.BanidoAte == null || m.BanidoAte > agora))
                            .Select(m => m.BanidoAte)
                            .FirstOrDefault()
                })
                .ToListAsync();

            var list = rows.Select(x => new ComunidadeDto
            {
                Id = x.Id,
                Nome = x.Nome,
                Descricao = x.Descricao,
                LimiteMembros = x.LimiteMembros,
                IsPrivada = x.IsPrivada,
                IsCurrentUserBanned = x.IsCurrentUserBanned,
                MeuBanimentoAteUtc = x.MeuBanimentoAteUtc,
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var agora = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                var membroBan = await _context.ComunidadeMembros.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Status == "Banido");
                if (BanimentoAtivo(membroBan, agora))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        message = "Foste banido desta comunidade. Não podes ver o conteúdo enquanto o ban estiver ativo.",
                        comunidadeNome = c.Nome,
                        banidoAte = membroBan!.BanidoAte
                    });
                }
            }

            var dto = new ComunidadeDto
            {
                Id = c.Id,
                Nome = c.Nome,
                Descricao = c.Descricao,
                LimiteMembros = c.LimiteMembros,
                IsPrivada = c.IsPrivada,
                DataCriacao = c.DataCriacao,
                MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == c.Id && m.Status == "Ativo"),
                IsCurrentUserBanned = false,
                MeuBanimentoAteUtc = null,
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
                    LimiteMembros = form.LimiteMembros is > 0 ? form.LimiteMembros : null,
                    IsPrivada = form.IsPrivada,
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
                        LimiteMembros = entity.LimiteMembros,
                        IsPrivada = entity.IsPrivada,
                        DataCriacao = entity.DataCriacao,
                        MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == entity.Id && m.Status == "Ativo"),
                    BannerUrl = BannerUrlFromFileName(entity.BannerFileName, baseUrl),
                    IconUrl = IconUrlFromFileName(entity.IconFileName, baseUrl)
                    };

                    return CreatedAtAction(nameof(GetById), new { id = entity.Id }, dto);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex,
                    "DB error creating comunidade: {Inner}",
                    ex.InnerException?.Message ?? ex.Message);
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

            if (form.LimiteMembros is > 0)
            {
                var membrosAtuais = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == id);
                if (form.LimiteMembros.Value < membrosAtuais)
                    return BadRequest(new { message = "O limite de membros não pode ser inferior ao número atual de membros." });
            }

            comunidade.Nome = form.Nome.Trim();
            comunidade.Descricao = string.IsNullOrWhiteSpace(form.Descricao) ? null : form.Descricao.Trim();
            comunidade.LimiteMembros = form.LimiteMembros is > 0 ? form.LimiteMembros : null;
            comunidade.IsPrivada = form.IsPrivada;

            if (form.Banner != null && form.Banner.Length > 0)
            {
                DeleteImageFile(comunidade.BannerFileName, "comunidades");
                comunidade.BannerFileName = await SaveImageAsync(form.Banner, "comunidades");
            }

            if (form.RemoveBanner)
            {
                DeleteImageFile(comunidade.BannerFileName, "comunidades");
                comunidade.BannerFileName = null;
            }

            if (form.Icon != null && form.Icon.Length > 0)
            {
                DeleteImageFile(comunidade.IconFileName, "comunidades/icons");
                comunidade.IconFileName = await SaveImageAsync(form.Icon, "comunidades/icons");
            }

            if (form.RemoveIcon)
            {
                DeleteImageFile(comunidade.IconFileName, "comunidades/icons");
                comunidade.IconFileName = null;
            }

                    await _context.SaveChangesAsync();

            var baseUrl = PublicBaseUrl();
            var dto = new ComunidadeDto
            {
                Id = comunidade.Id,
                Nome = comunidade.Nome,
                Descricao = comunidade.Descricao,
                LimiteMembros = comunidade.LimiteMembros,
                IsPrivada = comunidade.IsPrivada,
                DataCriacao = comunidade.DataCriacao,
                MembrosCount = await _context.ComunidadeMembros.CountAsync(m => m.ComunidadeId == comunidade.Id && m.Status == "Ativo"),
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueio = await ForbidSeBanidoAsync(id, userId);
            if (bloqueio != null) return bloqueio;

            var membros = await _context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.ComunidadeId == id && m.Status == "Ativo")
                .Select(m => new MembroDto
                {
                    UtilizadorId = m.UtilizadorId,
                    UserName = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == m.UtilizadorId)
                        .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    Role = m.Role,
                    Status = m.Status,
                    DataEntrada = m.DataEntrada,
                    CastigadoAte = m.CastigadoAte,
                    BanidoAte = m.BanidoAte,
                    MotivoBan = m.MotivoBan
                })
                .ToListAsync();

            // Fetch user tags and medal descriptions
            var userIds = membros.Select(m => m.UtilizadorId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (userIds.Count > 0)
            {
                var userData = await _context.Set<Utilizador>()
                    .Where(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserTag, u.UserTagPrimaryColor, u.UserTagSecondaryColor })
                    .ToListAsync();

                var tags = userData.Select(x => x.UserTag).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                var medalDescriptions = await _context.Medalhas
                    .ToListAsync();
                var descByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.Descricao,
                    StringComparer.OrdinalIgnoreCase);
                var iconByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.IconeUrl,
                    StringComparer.OrdinalIgnoreCase);

                var tagByUserId = userData.ToDictionary(x => x.Id, x => x.UserTag);
                var tagPrimaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagPrimaryColor);
                var tagSecondaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagSecondaryColor);

                foreach (var membro in membros)
                {
                    if (!string.IsNullOrEmpty(membro.UtilizadorId) && tagByUserId.TryGetValue(membro.UtilizadorId, out var tag))
                    {
                        membro.UserTag = tag;
                        if (!string.IsNullOrEmpty(tag) && descByTag.TryGetValue(tag, out var desc))
                        {
                            membro.UserTagDescription = desc;
                        }
                        if (!string.IsNullOrEmpty(tag) && iconByTag.TryGetValue(tag, out var icon))
                        {
                            membro.UserTagIconUrl = icon;
                        }
                        if (tagPrimaryColorByUserId.TryGetValue(membro.UtilizadorId, out var primaryColor))
                        {
                            membro.UserTagPrimaryColor = primaryColor;
                        }
                        if (tagSecondaryColorByUserId.TryGetValue(membro.UtilizadorId, out var secondaryColor))
                        {
                            membro.UserTagSecondaryColor = secondaryColor;
                        }
                    }
                }
            }

            return Ok(membros);
        }

        /// FR56 – Ranking interno da comunidade.
        /// Ordena os membros por número de filmes vistos (metrica=filmes, default)
        /// ou por minutos totais assistidos (metrica=tempo).
        /// O utilizador autenticado é identificado com IsCurrentUser = true.
        [HttpGet("{id:int}/ranking")]
        public async Task<IActionResult> GetRanking(int id, [FromQuery] string metrica = "filmes")
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueio = await ForbidSeBanidoAsync(id, userId);
            if (bloqueio != null) return bloqueio;

            var existe = await _context.Comunidades.AnyAsync(c => c.Id == id);
            if (!existe) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue("sub");

            var membros = await _context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.ComunidadeId == id && m.Status == "Ativo")
                .Select(m => m.UtilizadorId)
                .ToListAsync();

            if (membros.Count == 0)
                return Ok(Array.Empty<RankingMembroDto>());

            var utilizadores = await _context.Users
                .OfType<Utilizador>()
                .AsNoTracking()
                .Where(u => membros.Contains(u.Id))
                .Select(u => new { u.Id, NomeCompleto = !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome })
                .ToDictionaryAsync(u => u.Id, u => u.NomeCompleto ?? string.Empty);

            var stats = await _context.UserMovies
                .AsNoTracking()
                .Where(um => membros.Contains(um.UtilizadorId) && um.JaViu)
                .GroupBy(um => um.UtilizadorId)
                .Select(g => new
                {
                    UtilizadorId = g.Key,
                    FilmesVistos = g.Count(),
                    MinutosAssistidos = g
                        .Join(_context.Filmes,
                              um => um.FilmeId,
                              f => f.Id,
                              (um, f) => f.Duracao)
                        .Sum()
                })
                .ToDictionaryAsync(x => x.UtilizadorId, x => x);

            var lista = membros.Select(uid =>
            {
                stats.TryGetValue(uid, out var s);
                utilizadores.TryGetValue(uid, out var nome);
                return new
                {
                    UtilizadorId = uid,
                    UserName = nome ?? "Utilizador removido",
                    FilmesVistos = s?.FilmesVistos ?? 0,
                    MinutosAssistidos = s?.MinutosAssistidos ?? 0
                };
            }).ToList();

            var ordenada = metrica.ToLowerInvariant() == "tempo"
                ? lista.OrderByDescending(x => x.MinutosAssistidos).ThenByDescending(x => x.FilmesVistos)
                : lista.OrderByDescending(x => x.FilmesVistos).ThenByDescending(x => x.MinutosAssistidos);

            var resultado = ordenada
                .Select((x, idx) => new RankingMembroDto
                {
                    Posicao = idx + 1,
                    UtilizadorId = x.UtilizadorId,
                    UserName = x.UserName,
                    FilmesVistos = x.FilmesVistos,
                    MinutosAssistidos = x.MinutosAssistidos,
                    IsCurrentUser = x.UtilizadorId == currentUserId
                })
                .ToList();

            return Ok(resultado);
        }


        // ─── GET posts da comunidade ──────
        [HttpGet("{id:int}/posts")]
        public async Task<IActionResult> GetPosts(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string sortOrder = "desc")
        {
            var baseUrl = PublicBaseUrl();
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueio = await ForbidSeBanidoAsync(id, currentUserId);
            if (bloqueio != null) return bloqueio;

            var query = _context.ComunidadePosts.Where(p => p.ComunidadeId == id);

            // Apply global sorting
            switch ((sortOrder ?? "desc").ToLower())
            {
                case "asc":
                    query = query.OrderBy(p => p.DataCriacao);
                    break;
                case "likes":
                    query = query.OrderByDescending(p => _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && v.IsLike));
                    break;
                case "dislikes":
                    query = query.OrderByDescending(p => _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && !v.IsLike));
                    break;
                case "reports":
                    query = query.OrderByDescending(p => _context.ComunidadePostReports.Count(r => r.PostId == p.Id));
                    break;
                case "desc":
                default:
                    query = query.OrderByDescending(p => p.DataCriacao);
                    break;
            }

            var totalCount = await query.CountAsync();

            var posts = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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
                        .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    AutorFotoPerfilUrl = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == p.UtilizadorId)
                        .Select(u => u.FotoPerfilUrl)
                        .FirstOrDefault(),

                    LikesCount = _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && v.IsLike),
                    DislikesCount = _context.ComunidadePostVotos.Count(v => v.PostId == p.Id && !v.IsLike),

                    ReportsCount = _context.ComunidadePostReports.Count(r => r.PostId == p.Id),

                    ComentariosCount = _context.ComunidadePostComentarios.Count(c => c.PostId == p.Id),

                    FilmeId = p.FilmeId,
                    FilmeTitulo = p.FilmeTitulo,
                    FilmePosterUrl = p.FilmePosterUrl,

                    UserVote = currentUserId == null ? 0 :
                        _context.ComunidadePostVotos
                        .Where(v => v.PostId == p.Id && v.UtilizadorId == currentUserId)
                        .Select(v => v.IsLike ? 1 : -1)
                        .FirstOrDefault(),

                    JaReportou = currentUserId == null ? false :
                        _context.ComunidadePostReports
                        .Any(r => r.PostId == p.Id && r.UtilizadorId == currentUserId),

                    TemSpoiler = p.TemSpoiler
                })
                .ToListAsync();

            // Fetch user tags for post authors
            var authorIds = posts.Select(p => p.AutorId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (authorIds.Count > 0)
            {
                var userData = await _context.Set<Utilizador>()
                    .Where(u => authorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserTag, u.UserTagPrimaryColor, u.UserTagSecondaryColor })
                    .ToListAsync();

                var tags = userData.Select(x => x.UserTag).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                var medalDescriptions = await _context.Medalhas.ToListAsync();
                var descByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.Descricao,
                    StringComparer.OrdinalIgnoreCase);
                var iconByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.IconeUrl,
                    StringComparer.OrdinalIgnoreCase);

                var tagByUserId = userData.ToDictionary(x => x.Id, x => x.UserTag);
                var tagPrimaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagPrimaryColor);
                var tagSecondaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagSecondaryColor);

                foreach (var post in posts)
                {
                    if (!string.IsNullOrEmpty(post.AutorId) && tagByUserId.TryGetValue(post.AutorId, out var tag))
                    {
                        post.AutorUserTag = tag;
                        if (!string.IsNullOrEmpty(tag) && descByTag.TryGetValue(tag, out var desc))
                        {
                            post.AutorUserTagDescription = desc;
                        }
                        if (!string.IsNullOrEmpty(tag) && iconByTag.TryGetValue(tag, out var icon))
                        {
                            post.AutorUserTagIconUrl = icon;
                        }
                        if (tagPrimaryColorByUserId.TryGetValue(post.AutorId, out var primaryColor))
                        {
                            post.AutorUserTagPrimaryColor = primaryColor;
                        }
                        if (tagSecondaryColorByUserId.TryGetValue(post.AutorId, out var secondaryColor))
                        {
                            post.AutorUserTagSecondaryColor = secondaryColor;
                        }
                    }
                }
            }

            return Ok(new PaginatedPostsDto { Posts = posts, TotalCount = totalCount });
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
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Status == "Ativo");

            if (!isMembro) return Forbid();

            var membroInfo = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (MembroCastigadoAtivo(membroInfo, DateTime.UtcNow))
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
                TemSpoiler = form.TemSpoiler,
                FilmeId = form.FilmeId,           
                FilmeTitulo = form.FilmeTitulo,  
                FilmePosterUrl = form.FilmePosterUrl
            };

            _context.ComunidadePosts.Add(post);
            await _context.SaveChangesAsync();

            // ── Notify all community members (except the post author) ──
            try
            {
                var memberIds = await _context.ComunidadeMembros
                    .Where(m => m.ComunidadeId == id && m.Status == "Ativo" && m.UtilizadorId != userId)
                    .Select(m => m.UtilizadorId)
                    .ToListAsync();

                var notifications = memberIds.Select(mid => new NotificacaoComunidade
                {
                    UtilizadorId = mid,
                    ComunidadeId = id,
                    PostId = post.Id,
                    Tipo = "post",
                    CriadaEm = DateTime.UtcNow
                }).ToList();

                if (notifications.Count > 0)
                {
                    _context.Set<NotificacaoComunidade>().AddRange(notifications);
                        await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create community post notifications for comunidade {Id}", id);
                // Don't fail the post creation if notifications fail
            }

            try
            {
                var autor = await _context.Users
                    .OfType<Utilizador>()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var baseUrl = PublicBaseUrl();
                
                // Fetch medal description if user has a tag
                string? tagDescription = null;
                if (!string.IsNullOrEmpty(autor?.UserTag))
                {
                    var medal = await _context.Medalhas
                        .FirstOrDefaultAsync(m => m.Nome == autor.UserTag);
                    tagDescription = medal?.Descricao;
                }

                var dtoCompleto = new PostDto
                {
                    Id = post.Id,
                    Titulo = post.Titulo,
                    Conteudo = post.Conteudo,
                    DataCriacao = post.DataCriacao,
                    AutorId = userId,
                    AutorNome = autor != null ? (!string.IsNullOrEmpty(autor.UserName) && !autor.UserName.Contains("@") ? autor.UserName : autor.Nome + " " + autor.Sobrenome) : "Desconhecido",
                    ImagemUrl = post.ImagemUrl != null ? $"{baseUrl}{post.ImagemUrl}" : null,

                    LikesCount = 0,
                    DislikesCount = 0,
                    UserVote = 0,
                    ReportsCount = 0,
                    ComentariosCount = 0,
                    TemSpoiler = post.TemSpoiler,
                    JaReportou = false,

                    FilmeId = post.FilmeId,
                    FilmeTitulo = post.FilmeTitulo,
                    FilmePosterUrl = post.FilmePosterUrl,

                    AutorUserTag = autor?.UserTag,
                    AutorUserTagDescription = tagDescription,
                    AutorUserTagIconUrl = autor?.UserTag != null ? (await _context.Medalhas.FirstOrDefaultAsync(m => m.Nome == autor.UserTag))?.IconeUrl : null,
                    AutorUserTagPrimaryColor = autor?.UserTagPrimaryColor,
                    AutorUserTagSecondaryColor = autor?.UserTagSecondaryColor,
                    AutorFotoPerfilUrl = autor?.FotoPerfilUrl
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
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var comunidade = await _context.Comunidades.FirstOrDefaultAsync(c => c.Id == id);
            if (comunidade == null) return NotFound(new { message = "Comunidade não encontrada." });

            var agoraJuntar = DateTime.UtcNow;
            var banExpirado = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Status == "Banido"
                    && m.BanidoAte.HasValue && m.BanidoAte <= agoraJuntar);
            if (banExpirado != null)
            {
                _context.ComunidadeMembros.Remove(banExpirado);
                await _context.SaveChangesAsync();
            }

            var jaExiste = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            if (jaExiste)
            {
                var estadoRow = await _context.ComunidadeMembros
                    .AsNoTracking()
                    .Where(m => m.ComunidadeId == id && m.UtilizadorId == userId)
                    .Select(m => new { m.Status, m.BanidoAte })
                    .FirstOrDefaultAsync();
                if (estadoRow?.Status == "Banido" && (estadoRow.BanidoAte == null || estadoRow.BanidoAte > agoraJuntar))
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Foste banido desta comunidade." });
                return Conflict(new { message = "Já és membro desta comunidade." });
            }

            if (comunidade.IsPrivada)
            {
                var jaPendente = await _context.ComunidadePedidosEntrada
                    .AnyAsync(p => p.ComunidadeId == id && p.UtilizadorId == userId && p.Status == "Pendente");
                if (jaPendente)
                    return Conflict(new { message = "Já tens um pedido pendente para esta comunidade.", pendingApproval = true });

                var pedido = new ComunidadePedidoEntrada
                {
                    ComunidadeId = id,
                    UtilizadorId = userId,
                    Status = "Pendente",
                    DataPedido = DateTime.UtcNow
                };

                _context.ComunidadePedidosEntrada.Add(pedido);
                await _context.SaveChangesAsync();

                try
                {
                    var adminIds = await _context.ComunidadeMembros
                        .Where(m => m.ComunidadeId == id && m.Role == "Admin" && m.Status == "Ativo")
                        .Select(m => m.UtilizadorId)
                        .ToListAsync();

                    if (adminIds.Count > 0)
                    {
                        var notifs = adminIds.Select(adminId => new NotificacaoComunidade
                        {
                            UtilizadorId = adminId,
                            ComunidadeId = id,
                            PostId = null,
                            Tipo = "pedido_entrada",
                            CriadaEm = DateTime.UtcNow
                        }).ToList();

                        _context.NotificacoesComunidade.AddRange(notifs);
                        await _context.SaveChangesAsync();
                    }
            }
            catch (Exception ex)
            {
                    _logger.LogWarning(ex, "Falha ao criar notificação de pedido de entrada para admins da comunidade {Id}", id);
                }

                return Accepted(new { message = "Pedido enviado para aprovação do admin.", pendingApproval = true });
            }

            if (await IsLimiteAtingidoAsync(id, comunidade.LimiteMembros))
                return Conflict(new { message = "Esta comunidade já atingiu o limite de membros." });

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

        [Authorize]
        [HttpGet("{id:int}/me/estado")]
        public async Task<IActionResult> GetMeuEstado(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var agora = DateTime.UtcNow;
            var membro = await _context.ComunidadeMembros
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);

            var banidoAtivo = membro != null && membro.Status == "Banido"
                && (membro.BanidoAte == null || membro.BanidoAte > agora);
            var membroAtivo = membro != null && membro.Status == "Ativo";

            DateTime? castigadoAteAtivo = null;
            if (membroAtivo && MembroCastigadoAtivo(membro, agora))
                castigadoAteAtivo = membro!.CastigadoAte;

            var pedidoPendente = await _context.ComunidadePedidosEntrada
                .AsNoTracking()
                .AnyAsync(p => p.ComunidadeId == id && p.UtilizadorId == userId && p.Status == "Pendente");

            return Ok(new
            {
                isMembro = membroAtivo,
                isAdmin = membroAtivo && membro!.Role == "Admin",
                pedidoPendente,
                isBanned = banidoAtivo,
                banidoAte = banidoAtivo ? membro!.BanidoAte : null,
                castigadoAte = castigadoAteAtivo
            });
        }

        [Authorize]
        [HttpGet("{id:int}/pedidos")]
        public async Task<IActionResult> GetPedidosEntrada(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueioPedidos = await ForbidSeBanidoAsync(id, userId);
            if (bloqueioPedidos != null) return bloqueioPedidos;

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin" && m.Status == "Ativo");
            if (!isAdmin) return Forbid();

            var pedidos = await _context.ComunidadePedidosEntrada
                .AsNoTracking()
                .Where(p => p.ComunidadeId == id && p.Status == "Pendente")
                .OrderByDescending(p => p.DataPedido)
                .Select(p => new ComunidadePedidoEntradaDto
                {
                    Id = p.Id,
                    ComunidadeId = p.ComunidadeId,
                    UtilizadorId = p.UtilizadorId,
                    UserName = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == p.UtilizadorId)
                        .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    DataPedido = p.DataPedido
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        [Authorize]
        [HttpPost("{id:int}/pedidos/{pedidoId:int}/aprovar")]
        public async Task<IActionResult> AprovarPedidoEntrada(int id, int pedidoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueioApr = await ForbidSeBanidoAsync(id, userId);
            if (bloqueioApr != null) return bloqueioApr;

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin" && m.Status == "Ativo");
            if (!isAdmin) return Forbid();

            var comunidade = await _context.Comunidades.FirstOrDefaultAsync(c => c.Id == id);
            if (comunidade == null) return NotFound(new { message = "Comunidade não encontrada." });

            var pedido = await _context.ComunidadePedidosEntrada
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.ComunidadeId == id && p.Status == "Pendente");
            if (pedido == null) return NotFound(new { message = "Pedido não encontrado." });

            if (await IsLimiteAtingidoAsync(id, comunidade.LimiteMembros))
                return Conflict(new { message = "Não é possível aprovar: limite de membros atingido." });

            var jaMembro = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == pedido.UtilizadorId);
            if (!jaMembro)
            {
                _context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = id,
                    UtilizadorId = pedido.UtilizadorId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
            }

            pedido.Status = "Aprovado";
            pedido.DataResposta = DateTime.UtcNow;
            pedido.RespondidoPorId = userId;

            _context.NotificacoesComunidade.Add(new NotificacaoComunidade
            {
                UtilizadorId = pedido.UtilizadorId,
                ComunidadeId = id,
                PostId = null,
                Tipo = "pedido_aprovado",
                CriadaEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Pedido aprovado com sucesso." });
        }

        [Authorize]
        [HttpPost("{id:int}/pedidos/{pedidoId:int}/rejeitar")]
        public async Task<IActionResult> RejeitarPedidoEntrada(int id, int pedidoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueioRej = await ForbidSeBanidoAsync(id, userId);
            if (bloqueioRej != null) return bloqueioRej;

            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Role == "Admin" && m.Status == "Ativo");
            if (!isAdmin) return Forbid();

            var pedido = await _context.ComunidadePedidosEntrada
                .FirstOrDefaultAsync(p => p.Id == pedidoId && p.ComunidadeId == id && p.Status == "Pendente");
            if (pedido == null) return NotFound(new { message = "Pedido não encontrado." });

            pedido.Status = "Rejeitado";
            pedido.DataResposta = DateTime.UtcNow;
            pedido.RespondidoPorId = userId;

            _context.NotificacoesComunidade.Add(new NotificacaoComunidade
            {
                UtilizadorId = pedido.UtilizadorId,
                ComunidadeId = id,
                PostId = null,
                Tipo = "pedido_rejeitado",
                CriadaEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Pedido rejeitado." });
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
        public Task<IActionResult> RemoverMembro(int id, string utilizadorId) =>
            RemoverMembroComMotivo(id, utilizadorId, null);

        /// <summary>Expulsar membro, com motivo opcional (notificado ao utilizador).</summary>
        [Authorize]
        [HttpPost("{id:int}/membros/{utilizadorId}/expulsar")]
        public Task<IActionResult> ExpulsarMembro(int id, string utilizadorId, [FromBody] ExpulsarMembroForm? form) =>
            RemoverMembroComMotivo(id, utilizadorId, string.IsNullOrWhiteSpace(form?.Motivo) ? null : form!.Motivo!.Trim());

        private async Task<IActionResult> RemoverMembroComMotivo(int id, string utilizadorId, string? motivo)
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

            _context.NotificacoesComunidade.Add(new NotificacaoComunidade
            {
                UtilizadorId = utilizadorId,
                ComunidadeId = id,
                PostId = null,
                Tipo = "kick",
                Corpo = BuildKickCorpo(motivo),
                CriadaEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Membro e respetivo histórico removidos com sucesso." });
        }

        [Authorize]
        [HttpPost("{id:int}/membros/{utilizadorId}/banir")]
        public async Task<IActionResult> BanirMembro(int id, string utilizadorId, [FromBody] BanirMembroForm? form)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == currentUserId && m.Role == "Admin");
            if (!isAdmin) return Forbid();
            if (currentUserId == utilizadorId) return BadRequest(new { message = "Não te podes banir a ti próprio." });

            var motivoBan = string.IsNullOrWhiteSpace(form?.Motivo) ? null : form!.Motivo!.Trim();
            DateTime? banidoAte = null;
            if (form?.DuracaoDias is int dias && dias > 0)
            {
                if (dias > 3650) dias = 3650;
                banidoAte = DateTime.UtcNow.AddDays(dias);
            }

            var membro = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == utilizadorId);

            if (membro == null)
            {
                _context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = id,
                    UtilizadorId = utilizadorId,
                    Role = "Membro",
                    Status = "Banido",
                    DataEntrada = DateTime.UtcNow,
                    BanidoAte = banidoAte,
                    MotivoBan = motivoBan
                });
            }
            else
            {
                membro.Status = "Banido";
                membro.CastigadoAte = null;
                membro.BanidoAte = banidoAte;
                membro.MotivoBan = motivoBan;
            }

            _context.NotificacoesComunidade.Add(new NotificacaoComunidade
            {
                UtilizadorId = utilizadorId,
                ComunidadeId = id,
                PostId = null,
                Tipo = "banido",
                Corpo = BuildBanCorpo(banidoAte, motivoBan),
                CriadaEm = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Membro banido com sucesso." });
        }

        [Authorize]
        [HttpGet("{id:int}/banidos")]
        public async Task<IActionResult> GetBanidos(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var isAdmin = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == currentUserId && m.Role == "Admin");
            if (!isAdmin) return Forbid();

            var banidos = await _context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.ComunidadeId == id && m.Status == "Banido")
                .Select(m => new MembroDto
                {
                    UtilizadorId = m.UtilizadorId,
                    UserName = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == m.UtilizadorId)
                        .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    Role = m.Role,
                    Status = m.Status,
                    DataEntrada = m.DataEntrada,
                    BanidoAte = m.BanidoAte,
                    MotivoBan = m.MotivoBan
                })
                .ToListAsync();

            var banidosUserIds = banidos.Select(m => m.UtilizadorId).Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (banidosUserIds.Count > 0)
            {
                var userData = await _context.Set<Utilizador>()
                    .Where(u => banidosUserIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserTag, u.UserTagPrimaryColor, u.UserTagSecondaryColor })
                    .ToListAsync();

                var tags = userData.Select(x => x.UserTag).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                var medalDescriptions = await _context.Medalhas
                    .ToListAsync();
                var descByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.Descricao,
                    StringComparer.OrdinalIgnoreCase);
                var iconByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.IconeUrl,
                    StringComparer.OrdinalIgnoreCase);

                var tagByUserId = userData.ToDictionary(x => x.Id, x => x.UserTag);
                var tagPrimaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagPrimaryColor);
                var tagSecondaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagSecondaryColor);

                foreach (var membro in banidos)
                {
                    if (!string.IsNullOrEmpty(membro.UtilizadorId) && tagByUserId.TryGetValue(membro.UtilizadorId, out var tag))
                    {
                        membro.UserTag = tag;
                        if (!string.IsNullOrEmpty(tag) && descByTag.TryGetValue(tag, out var desc))
                        {
                            membro.UserTagDescription = desc;
                        }
                        if (!string.IsNullOrEmpty(tag) && iconByTag.TryGetValue(tag, out var icon))
                        {
                            membro.UserTagIconUrl = icon;
                        }
                        if (tagPrimaryColorByUserId.TryGetValue(membro.UtilizadorId, out var primaryColor))
                        {
                            membro.UserTagPrimaryColor = primaryColor;
                        }
                        if (tagSecondaryColorByUserId.TryGetValue(membro.UtilizadorId, out var secondaryColor))
                        {
                            membro.UserTagSecondaryColor = secondaryColor;
                        }
                    }
                }
            }

            return Ok(banidos);
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

            var postOk = await _context.ComunidadePosts.AsNoTracking()
                .AnyAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (!postOk) return NotFound();

            var agora = DateTime.UtcNow;
            var membroVoto = await _context.ComunidadeMembros.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);
            if (membroVoto == null || membroVoto.Status != "Ativo") return Forbid();
            if (MembroCastigadoAtivo(membroVoto, agora))
                return BadRequest(new { message = "Estás castigado e não podes votar nas publicações." });

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
        public async Task<IActionResult> EditPost(int id, int postId, [FromBody] PostDto form)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            var post = await _context.ComunidadePosts.FirstOrDefaultAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (post == null) return NotFound();

            if (post.UtilizadorId != userId) return Forbid();

            var membroEdit = await _context.ComunidadeMembros.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);
            if (MembroCastigadoAtivo(membroEdit, DateTime.UtcNow))
                return BadRequest(new { message = "Estás castigado e não podes editar publicações." });

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

            var membroDel = await _context.ComunidadeMembros.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);
            if (MembroCastigadoAtivo(membroDel, DateTime.UtcNow))
                return BadRequest(new { message = "Estás castigado e não podes apagar publicações." });

            _context.ComunidadePosts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Publicação apagada com sucesso." });
        }

        // ─── GET comentários de um post ────
        [HttpGet("{id:int}/posts/{postId:int}/comentarios")]
        public async Task<IActionResult> GetComentarios(int id, int postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var bloqueio = await ForbidSeBanidoAsync(id, userId);
            if (bloqueio != null) return bloqueio;

            var query = _context.ComunidadePostComentarios.Where(c => c.PostId == postId);
            var totalCount = await query.CountAsync();

            var comentarios = await query
                .OrderByDescending(c => c.DataCriacao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Conteudo,
                    c.DataCriacao,
                    AutorId = c.UtilizadorId,
                    AutorNome = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == c.UtilizadorId)
                        .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                        .FirstOrDefault() ?? "Utilizador removido",
                    AutorFotoPerfilUrl = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == c.UtilizadorId)
                        .Select(u => u.FotoPerfilUrl)
                        .FirstOrDefault(),
                    AutorUserTag = _context.Users.OfType<Utilizador>()
                        .Where(u => u.Id == c.UtilizadorId)
                        .Select(u => u.UserTag)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // Fetch medal descriptions, icons, and colors for comment authors
            var authorIds = comentarios.Select(c => c.AutorId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (authorIds.Count > 0)
            {
                var userData = await _context.Set<Utilizador>()
                    .Where(u => authorIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.UserTag, u.UserTagPrimaryColor, u.UserTagSecondaryColor })
                    .ToListAsync();

                var tags = userData.Select(x => x.UserTag).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                var medalDescriptions = await _context.Medalhas.ToListAsync();
                var descByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.Descricao,
                    StringComparer.OrdinalIgnoreCase);
                var iconByTag = medalDescriptions.ToDictionary(
                    x => x.Nome,
                    x => x.IconeUrl,
                    StringComparer.OrdinalIgnoreCase);

                var tagByUserId = userData.ToDictionary(x => x.Id, x => x.UserTag);
                var tagPrimaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagPrimaryColor);
                var tagSecondaryColorByUserId = userData.ToDictionary(x => x.Id, x => x.UserTagSecondaryColor);

                var comentariosComTags = comentarios.Select(c => new
                {
                    c.Id,
                    c.Conteudo,
                    c.DataCriacao,
                    c.AutorId,
                    c.AutorNome,
                    c.AutorFotoPerfilUrl,
                    c.AutorUserTag,
                    AutorUserTagDescription = !string.IsNullOrEmpty(c.AutorUserTag) && descByTag.TryGetValue(c.AutorUserTag, out var desc) ? desc : null,
                    AutorUserTagIconUrl = !string.IsNullOrEmpty(c.AutorUserTag) && iconByTag.TryGetValue(c.AutorUserTag, out var icon) ? icon : null,
                    AutorUserTagPrimaryColor = !string.IsNullOrEmpty(c.AutorId) && tagPrimaryColorByUserId.TryGetValue(c.AutorId, out var primaryColor) ? primaryColor : null,
                    AutorUserTagSecondaryColor = !string.IsNullOrEmpty(c.AutorId) && tagSecondaryColorByUserId.TryGetValue(c.AutorId, out var secondaryColor) ? secondaryColor : null
                }).Cast<object>().ToList();

                return Ok(new PaginatedComunidadeCommentsDto { Comments = comentariosComTags, TotalCount = totalCount });
            }

            return Ok(new PaginatedComunidadeCommentsDto { Comments = comentarios.Cast<object>().ToList(), TotalCount = totalCount });
        }

        // ─── POST criar comentário ────
        [Authorize]
        [HttpPost("{id:int}/posts/{postId:int}/comentarios")]
        public async Task<IActionResult> CreateComentario(int id, int postId, [FromBody] CreateComentarioDto form)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (string.IsNullOrWhiteSpace(form.Conteudo))
                return BadRequest(new { message = "O conteúdo do comentário é obrigatório." });

            var postExists = await _context.ComunidadePosts.AnyAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (!postExists) return NotFound();

            var isMembro = await _context.ComunidadeMembros
                .AnyAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId && m.Status == "Ativo");
            if (!isMembro) return Forbid();

            var membroInfo = await _context.ComunidadeMembros
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);
            if (MembroCastigadoAtivo(membroInfo, DateTime.UtcNow))
            {
                return BadRequest(new { message = "Estás temporariamente impedido de comentar." });
            }

            var comentario = new ComunidadePostComentario
            {
                PostId = postId,
                UtilizadorId = userId,
                Conteudo = form.Conteudo.Trim(),
                DataCriacao = DateTime.UtcNow
            };

            _context.ComunidadePostComentarios.Add(comentario);
                        await _context.SaveChangesAsync();

            var autor = await _context.Users.OfType<Utilizador>()
                .FirstOrDefaultAsync(u => u.Id == userId);

            string? tagDescription = null;
            string? tagIconUrl = null;
            if (!string.IsNullOrEmpty(autor?.UserTag))
            {
                var medal = await _context.Medalhas
                    .FirstOrDefaultAsync(m => m.Nome == autor.UserTag);
                tagDescription = medal?.Descricao;
                tagIconUrl = medal?.IconeUrl;
            }

            var comentarioCriado = new
            {
                Id = comentario.Id,
                Conteudo = comentario.Conteudo,
                DataCriacao = comentario.DataCriacao,
                AutorId = userId,
                AutorNome = autor != null ? (!string.IsNullOrEmpty(autor.UserName) && !autor.UserName.Contains("@") ? autor.UserName : autor.Nome + " " + autor.Sobrenome) : "Desconhecido",
                AutorFotoPerfilUrl = autor?.FotoPerfilUrl,
                AutorUserTag = autor?.UserTag,
                AutorUserTagDescription = tagDescription,
                AutorUserTagIconUrl = tagIconUrl,
                AutorUserTagPrimaryColor = autor?.UserTagPrimaryColor,
                AutorUserTagSecondaryColor = autor?.UserTagSecondaryColor
            };

            return Ok(comentarioCriado);
        }

        // ─── POST Reportar Post ────
        [Authorize]
        [HttpPost("{id:int}/posts/{postId:int}/report")]
        public async Task<IActionResult> ReportPost(int id, int postId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var post = await _context.ComunidadePosts.AnyAsync(p => p.Id == postId && p.ComunidadeId == id);
            if (!post) return NotFound();

            var jaReportou = await _context.ComunidadePostReports
                .AnyAsync(r => r.PostId == postId && r.UtilizadorId == userId);

            if (jaReportou) 
                return BadRequest(new { message = "Já denunciaste esta publicação." });

            var membroReport = await _context.ComunidadeMembros.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ComunidadeId == id && m.UtilizadorId == userId);
            if (membroReport == null || membroReport.Status != "Ativo") return Forbid();
            if (MembroCastigadoAtivo(membroReport, DateTime.UtcNow))
                return BadRequest(new { message = "Estás castigado e não podes denunciar publicações." });

            _context.ComunidadePostReports.Add(new ComunidadePostReport
            {
                PostId = postId,
                UtilizadorId = userId
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Publicação denunciada com sucesso." });
        }

        // ─── DTOs & Forms ────

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
            public int ComentariosCount { get; set; }
            public bool TemSpoiler { get; set; }
            public bool JaReportou { get; set; }
            
            public int? FilmeId { get; set; }
            public string? FilmeTitulo { get; set; }
            public string? FilmePosterUrl { get; set; }

            // User medal tag for post author
            public string? AutorUserTag { get; set; }
            public string? AutorUserTagDescription { get; set; }
            public string? AutorUserTagIconUrl { get; set; }
            public string? AutorUserTagPrimaryColor { get; set; }
            public string? AutorUserTagSecondaryColor { get; set; }
            public string? AutorFotoPerfilUrl { get; set; }
        }

        public class CreateComentarioDto
        {
            public string Conteudo { get; set; } = "";
        }

        public class MembroDto
        {
            public string? UtilizadorId { get; set; }
            public string? UserName { get; set; }
            public string? Role { get; set; }
            public string? Status { get; set; }
            public DateTime DataEntrada { get; set; }
            public DateTime? CastigadoAte { get; set; }
            public DateTime? BanidoAte { get; set; }
            public string? MotivoBan { get; set; }
            public string? UserTag { get; set; }
            public string? UserTagDescription { get; set; }
            public string? UserTagIconUrl { get; set; }
            public string? UserTagPrimaryColor { get; set; }
            public string? UserTagSecondaryColor { get; set; }
        }

        public class ExpulsarMembroForm
        {
            public string? Motivo { get; set; }
        }

        public class BanirMembroForm
        {
            /// <summary>Se null ou &lt;= 0, banimento permanente (BanidoAte null).</summary>
            public int? DuracaoDias { get; set; }
            public string? Motivo { get; set; }
        }


        public class RankingMembroDto
        {
            public int Posicao { get; set; }
            public string? UtilizadorId { get; set; }
            public string? UserName { get; set; }
            public int FilmesVistos { get; set; }
            public int MinutosAssistidos { get; set; }
            public bool IsCurrentUser { get; set; }
        }

        public class ComunidadeDto
        {
            public int Id { get; set; }
            public string Nome { get; set; } = "";
            public string? Descricao { get; set; }
            public int? LimiteMembros { get; set; }
            public bool IsPrivada { get; set; }
            public bool IsCurrentUserBanned { get; set; }
            /// <summary>Se banido: fim do ban em UTC; null na lista significa banimento permanente.</summary>
            public DateTime? MeuBanimentoAteUtc { get; set; }
            public DateTime DataCriacao { get; set; }
            public int MembrosCount { get; set; }
            public string? BannerUrl { get; set; }
            public string? IconUrl { get; set; }
        }

        public class ComunidadePedidoEntradaDto
        {
            public int Id { get; set; }
            public int ComunidadeId { get; set; }
            public string UtilizadorId { get; set; } = "";
            public string UserName { get; set; } = "";
            public DateTime DataPedido { get; set; }
        }

        public class PostCreateForm
        {
            public string Titulo { get; set; } = "";
            public string Conteudo { get; set; } = "";
            public IFormFile? Imagem { get; set; }
            [FromForm(Name = "temSpoiler")]
            public bool TemSpoiler { get; set; }

            [FromForm(Name = "filmeId")]
            public int? FilmeId { get; set; }
            [FromForm(Name = "filmeTitulo")]
            public string? FilmeTitulo { get; set; }
            [FromForm(Name = "filmePosterUrl")]
            public string? FilmePosterUrl { get; set; }
        }

        public class ComunidadeCreateForm
        {
            [FromForm(Name = "nome")]
            public string Nome { get; set; } = "";

            [FromForm(Name = "descricao")]
            public string? Descricao { get; set; }

            [FromForm(Name = "limiteMembros")]
            public int? LimiteMembros { get; set; }

            [FromForm(Name = "isPrivada")]
            public bool IsPrivada { get; set; }

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

            [FromForm(Name = "limiteMembros")]
            public int? LimiteMembros { get; set; }

            [FromForm(Name = "isPrivada")]
            public bool IsPrivada { get; set; }

            [FromForm(Name = "banner")]
            public IFormFile? Banner { get; set; }

            [FromForm(Name = "icon")]
            public IFormFile? Icon { get; set; }

            [FromForm(Name = "removeBanner")]
            public bool RemoveBanner { get; set; }

            [FromForm(Name = "removeIcon")]
            public bool RemoveIcon { get; set; }
        }
        public class PaginatedPostsDto
        {
            public List<PostDto> Posts { get; set; } = new();
            public int TotalCount { get; set; }
        }

        public class PaginatedComunidadeCommentsDto
        {
            public List<object> Comments { get; set; } = new();
            public int TotalCount { get; set; }
        }
    }
}