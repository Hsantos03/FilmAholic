using System.Security.Claims;
using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class AdminController : ControllerBase
{
    private readonly FilmAholicDbContext _context;
    private readonly UserManager<Utilizador> _userManager;

    public AdminController(FilmAholicDbContext context, UserManager<Utilizador> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private string AdminUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

    // ─── Utilizadores (UC28) ───

    [HttpGet("utilizadores")]
    public async Task<IActionResult> ListarUtilizadores([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.UserName != null && u.UserName.Contains(term)) ||
                u.Nome.Contains(term) ||
                u.Sobrenome.Contains(term));
        }

        var total = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(new
            {
                u.Id,
                u.Email,
                u.UserName,
                u.Nome,
                u.Sobrenome,
                u.EmailConfirmed,
                u.LockoutEnd,
                roles
            });
        }

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("utilizadores/{id}")]
    public async Task<IActionResult> ObterUtilizador(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(u);
        return Ok(new
        {
            u.Id,
            u.Email,
            u.UserName,
            u.Nome,
            u.Sobrenome,
            u.EmailConfirmed,
            u.LockoutEnd,
            roles
        });
    }

    [HttpPut("utilizadores/{id}")]
    public async Task<IActionResult> AtualizarUtilizador(string id, [FromBody] AdminAtualizarUtilizadorDto dto)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Nome)) u.Nome = dto.Nome.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Sobrenome)) u.Sobrenome = dto.Sobrenome.Trim();

        var result = await _userManager.UpdateAsync(u);
        if (!result.Succeeded)
            return BadRequest(new { message = "Não foi possível atualizar.", errors = result.Errors.Select(e => e.Description).ToList() });

        return Ok(new { message = "Utilizador atualizado." });
    }

    [HttpPost("utilizadores/{id}/bloquear")]
    public async Task<IActionResult> BloquearUtilizador(string id)
    {
        var adminId = AdminUserId();
        if (id == adminId) return BadRequest(new { message = "Não podes bloquear a tua própria conta." });

        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        await _userManager.SetLockoutEnabledAsync(u, true);
        await _userManager.SetLockoutEndDateAsync(u, DateTimeOffset.UtcNow.AddYears(100));
        return Ok(new { message = "Conta bloqueada." });
    }

    [HttpPost("utilizadores/{id}/desbloquear")]
    public async Task<IActionResult> DesbloquearUtilizador(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(u, null);
        await _userManager.SetLockoutEnabledAsync(u, false);
        return Ok(new { message = "Conta desbloqueada." });
    }

    [HttpDelete("utilizadores/{id}")]
    public async Task<IActionResult> EliminarUtilizador(string id)
    {
        var adminId = AdminUserId();
        if (id == adminId) return BadRequest(new { message = "Não podes eliminar a tua própria conta." });

        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        var result = await _userManager.DeleteAsync(u);
        if (!result.Succeeded)
            return BadRequest(new { message = "Não foi possível eliminar (dependências ou erro).", errors = result.Errors.Select(e => e.Description).ToList() });

        return Ok(new { message = "Conta eliminada." });
    }

    // ─── Comunidades (UC29) ───

    [HttpGet("comunidades")]
    public async Task<IActionResult> ListarComunidades()
    {
        var list = await _context.Comunidades
            .AsNoTracking()
            .OrderBy(c => c.Nome)
            .Select(c => new
            {
                c.Id,
                c.Nome,
                MembrosAtivos = _context.ComunidadeMembros.Count(m => m.ComunidadeId == c.Id && m.Status == "Ativo"),
                Posts = _context.ComunidadePosts.Count(p => p.ComunidadeId == c.Id)
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("comunidades/{id:int}/posts")]
    public async Task<IActionResult> ListarPostsComunidade(int id)
    {
        if (!await _context.Comunidades.AnyAsync(c => c.Id == id))
            return NotFound();

        var posts = await _context.ComunidadePosts
            .AsNoTracking()
            .Where(p => p.ComunidadeId == id)
            .OrderByDescending(p => p.DataCriacao)
            .Take(150)
            .Select(p => new
            {
                p.Id,
                p.Titulo,
                p.DataCriacao,
                AutorId = p.UtilizadorId,
                AutorNome = _context.Users.OfType<Utilizador>()
                    .Where(u => u.Id == p.UtilizadorId)
                    .Select(u => !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome)
                    .FirstOrDefault() ?? "Utilizador removido"
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpGet("comunidades/{id:int}/membros")]
    public async Task<IActionResult> ListarMembrosComunidade(int id)
    {
        if (!await _context.Comunidades.AnyAsync(c => c.Id == id))
            return NotFound();

        var membros = await _context.ComunidadeMembros
            .AsNoTracking()
            .Where(m => m.ComunidadeId == id && m.Status == "Ativo")
            .Join(_context.Users, m => m.UtilizadorId, u => u.Id, (m, u) => new
            {
                utilizadorId = m.UtilizadorId,
                nome = !string.IsNullOrEmpty(u.UserName) && !u.UserName.Contains("@") ? u.UserName : u.Nome + " " + u.Sobrenome,
                email = u.Email,
                m.Role
            })
            .ToListAsync();

        return Ok(membros);
    }

    [HttpDelete("comunidades/{comunidadeId:int}/posts/{postId:int}")]
    public async Task<IActionResult> ApagarPostPlataforma(int comunidadeId, int postId)
    {
        var post = await _context.ComunidadePosts
            .FirstOrDefaultAsync(p => p.Id == postId && p.ComunidadeId == comunidadeId);
        if (post == null) return NotFound();

        _context.ComunidadePosts.Remove(post);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Publicação removida." });
    }

    [HttpDelete("comunidades/{comunidadeId:int}/membros/{utilizadorId}")]
    public async Task<IActionResult> RemoverMembroPlataforma(int comunidadeId, string utilizadorId, [FromQuery] string? motivo)
    {
        var adminId = AdminUserId();
        if (utilizadorId == adminId)
            return BadRequest(new { message = "Não podes remover-te a ti próprio como administrador da plataforma." });

        var membro = await _context.ComunidadeMembros
            .FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == utilizadorId);
        if (membro == null) return NotFound(new { message = "Membro não encontrado." });

        var votosNaComunidade = await _context.ComunidadePostVotos
            .Include(v => v.Post)
            .Where(v => v.UtilizadorId == utilizadorId && v.Post.ComunidadeId == comunidadeId)
            .ToListAsync();
        _context.ComunidadePostVotos.RemoveRange(votosNaComunidade);

        var reportsNaComunidade = await _context.ComunidadePostReports
            .Include(r => r.Post)
            .Where(r => r.UtilizadorId == utilizadorId && r.Post.ComunidadeId == comunidadeId)
            .ToListAsync();
        _context.ComunidadePostReports.RemoveRange(reportsNaComunidade);

        var postsDoUtilizador = await _context.ComunidadePosts
            .Where(p => p.UtilizadorId == utilizadorId && p.ComunidadeId == comunidadeId)
            .ToListAsync();
        _context.ComunidadePosts.RemoveRange(postsDoUtilizador);

        _context.ComunidadeMembros.Remove(membro);

        var corpoKick = string.IsNullOrWhiteSpace(motivo) ? null : $"Motivo: {motivo.Trim()}";
        _context.NotificacoesComunidade.Add(new NotificacaoComunidade
        {
            UtilizadorId = utilizadorId,
            ComunidadeId = comunidadeId,
            PostId = null,
            Tipo = "kick",
            Corpo = corpoKick,
            CriadaEm = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Membro removido da comunidade." });
    }

    // ─── Notificações globais (UC30) ───

    [HttpPost("notificacoes-globais")]
    public async Task<IActionResult> EnviarNotificacaoGlobal([FromBody] AdminNotificacaoGlobalDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo) || string.IsNullOrWhiteSpace(dto.Mensagem))
            return BadRequest(new { message = "Título e mensagem são obrigatórios." });

        var titulo = dto.Titulo.Trim();
        var mensagem = dto.Mensagem.Trim();
        if (titulo.Length > 200) return BadRequest(new { message = "Título demasiado longo." });
        if (mensagem.Length > 3500) return BadRequest(new { message = "Mensagem demasiado longa." });

        var corpo = JsonSerializer.Serialize(new { titulo, mensagem });

        var userIds = await _userManager.Users
            .Where(u => u.EmailConfirmed)
            .Select(u => u.Id)
            .ToListAsync();

        const int batchSize = 400;
        var criadas = 0;
        for (var i = 0; i < userIds.Count; i += batchSize)
        {
            var batch = userIds.Skip(i).Take(batchSize);
            foreach (var uid in batch)
            {
                _context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = uid,
                    Tipo = "AnuncioPlataforma",
                    Corpo = corpo,
                    CriadaEm = DateTime.UtcNow
                });
                criadas++;
            }

            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Notificação enviada.", destinatarios = criadas });
    }

    // ─── Desafios e recompensas — desafios (UC31) ───

    [HttpGet("desafios")]
    public async Task<IActionResult> ListarDesafios()
    {
        var list = await _context.Desafios
            .OrderByDescending(d => d.DataInicio)
            .ToListAsync();
        return Ok(list);
    }

    [HttpPost("desafios")]
    public async Task<IActionResult> CriarDesafio([FromBody] Desafio body)
    {
        body.Id = 0;
        _context.Desafios.Add(body);
        await _context.SaveChangesAsync();
        return Ok(body);
    }

    [HttpPut("desafios/{id:int}")]
    public async Task<IActionResult> AtualizarDesafio(int id, [FromBody] Desafio body)
    {
        var d = await _context.Desafios.FindAsync(id);
        if (d == null) return NotFound();

        d.DataInicio = body.DataInicio;
        d.DataFim = body.DataFim;
        d.Descricao = body.Descricao;
        d.Ativo = body.Ativo;
        d.Genero = body.Genero;
        d.QuantidadeNecessaria = body.QuantidadeNecessaria;
        d.Xp = body.Xp;
        d.Pergunta = body.Pergunta;
        d.OpcaoA = body.OpcaoA;
        d.OpcaoB = body.OpcaoB;
        d.OpcaoC = body.OpcaoC;
        d.RespostaCorreta = body.RespostaCorreta;

        await _context.SaveChangesAsync();
        return Ok(d);
    }

    [HttpDelete("desafios/{id:int}")]
    public async Task<IActionResult> EliminarDesafio(int id)
    {
        var d = await _context.Desafios.FindAsync(id);
        if (d == null) return NotFound();
        _context.Desafios.Remove(d);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Desafio eliminado." });
    }

    // ─── Medalhas (recompensas — edição básica) ───

    [HttpGet("medalhas")]
    public async Task<IActionResult> ListarMedalhas()
    {
        var list = await _context.Medalhas.AsNoTracking().OrderBy(m => m.Nome).ToListAsync();
        return Ok(list);
    }

    [HttpPut("medalhas/{id:int}")]
    public async Task<IActionResult> AtualizarMedalha(int id, [FromBody] AdminAtualizarMedalhaDto dto)
    {
        var m = await _context.Medalhas.FindAsync(id);
        if (m == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Nome)) m.Nome = dto.Nome.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Descricao)) m.Descricao = dto.Descricao.Trim();
        if (!string.IsNullOrWhiteSpace(dto.IconeUrl)) m.IconeUrl = dto.IconeUrl.Trim();
        if (dto.CriterioQuantidade is int q && q >= 0) m.CriterioQuantidade = q;
        if (!string.IsNullOrWhiteSpace(dto.CriterioTipo)) m.CriterioTipo = dto.CriterioTipo.Trim();
        if (dto.Ativa.HasValue) m.Ativa = dto.Ativa.Value;

        await _context.SaveChangesAsync();
        return Ok(m);
    }

    public class AdminAtualizarUtilizadorDto
    {
        public string? Nome { get; set; }
        public string? Sobrenome { get; set; }
    }

    public class AdminNotificacaoGlobalDto
    {
        public string Titulo { get; set; } = "";
        public string Mensagem { get; set; } = "";
    }

    public class AdminAtualizarMedalhaDto
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public string? IconeUrl { get; set; }
        public int? CriterioQuantidade { get; set; }
        public string? CriterioTipo { get; set; }
        public bool? Ativa { get; set; }
    }
}
