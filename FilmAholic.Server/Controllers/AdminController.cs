using System.Security.Claims;
using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers;

    /// <summary>
    /// Controlador central destinado para utilzadores com previlégios elevados (Admins).
    /// Pode-se gerir bloqueios, listas comunitárias, limpezas, envio de avisos globais e gerências de desafios e prémios.
    /// </summary>
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

        /// <summary>
        /// Método privado auxiliar para desencriptação ligeira do Id na sessão do administrador que comanda a ação.
        /// </summary>
        /// <returns>ID único de string retido.</returns>
        private string AdminUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

        // ─── Utilizadores (UC28) ───

        /// <summary>
        /// Lista de páginas do catálogo de utilizadores do projeto. Permite filtrar por nome ou correio eletrónico.
        /// </summary>
        /// <param name="q">Parte textual de uma query para busca flexível nas colunas visadas (opcional).</param>
        /// <param name="page">Índice atual de paginação a retornar (Por defeito: 1).</param>
        /// <param name="pageSize">Montante global de perfis trazidos da base em simultâneo (Balizado até 100).</param>
        /// <returns>Total agrupado listando o estado primário das contas, papéis e timestamps dos locks aplicados.</returns>
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

        /// <summary>
        /// Extrai uma única entidade de utilizador face ao número registado primário em Identity.
        /// </summary>
        /// <param name="id">Chave Guid injetada pela Microsoft Identity associada à conta.</param>
        /// <returns>Objeto contendo e-mail e dados da sessão de bloqueios exposta.</returns>
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

        /// <summary>
        /// Manipula os dados abertos superficiais de um civil/admin (Nome, Sobrenome).
        /// </summary>
        /// <param name="id">Assinatura GUID do sujeito submetido à alteração.</param>
        /// <param name="dto">Dados vindos da UI contendo a retificação final.</param>
        /// <returns>Sucesso se aplicou com distinção; lista de falhas providenciada pelo UserManager se der erro.</returns>
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

        /// <summary>
        /// Bane indefinidamente ou cessa o acesso do utilizador de interagir ou realizar novos logins à sua conta.
        /// </summary>
        /// <param name="id">A entidade suspensa.</param>
        /// <returns>Resposta afirmativa confirmando que o trancamento tomou palco e vigor nos próximos cem anos.</returns>
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

        /// <summary>
        /// Cancela o selo de encerramento temporário/permanente da conta de outros utilizadores.
        /// </summary>
        /// <param name="id">Id da pessoa retificada.</param>
        /// <returns>Confirma descerramento e retoma da normalidade ao perfil.</returns>
        [HttpPost("utilizadores/{id}/desbloquear")]
        public async Task<IActionResult> DesbloquearUtilizador(string id)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(u, null);
        await _userManager.SetLockoutEnabledAsync(u, false);
        return Ok(new { message = "Conta desbloqueada." });
    }

        /// <summary>
        /// Exclui todos os passos do utilizador, limpando referências no site.
        /// </summary>
        /// <param name="id">GUID exato do alvo.</param>
        /// <returns>Reporta erros cascata ou sucesso retórico de exclusão.</returns>
        [HttpDelete("utilizadores/{id}")]
        public async Task<IActionResult> EliminarUtilizador(string id)
    {
        var adminId = AdminUserId();
        if (id == adminId) return BadRequest(new { message = "Não podes eliminar a tua própria conta." });

        var u = await _userManager.FindByIdAsync(id);
        if (u == null) return NotFound();

        // Igual ao apagar a própria conta (ProfileController): manter comentários, anonimizar autor.
        var votesDoUser = _context.CommentVotes.Where(v => v.UserId == id);
        _context.CommentVotes.RemoveRange(votesDoUser);

        var comentariosDoUser = await _context.Comments.Where(c => c.UserId == id).ToListAsync();
        foreach (var comment in comentariosDoUser)
        {
            comment.UserName = "Conta Eliminada";
            comment.UserId = null;
        }

        await ComunidadeEliminacaoAoRemoverConta.ExecutarAsync(_context, id);

        var result = await _userManager.DeleteAsync(u);
        if (!result.Succeeded)
            return BadRequest(new { message = "Não foi possível eliminar (dependências ou erro).", errors = result.Errors.Select(e => e.Description).ToList() });

        await _context.SaveChangesAsync();

        return Ok(new { message = "Conta eliminada." });
    }

        // ─── Comunidades (UC29) ───

        /// <summary>
        /// Sumariza todas as comunidades ativamente publicadas e a contagem de posts e estatudo "Ativo".
        /// </summary>
        /// <returns>Lista estruturada com metadados de membros e threads nas salas comunitárias.</returns>
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

        /// <summary>
        /// Consulta dos posts contidos sob o máximo de publicações por comunidade (Restrito a 150 pela ótica da performance Admin).
        /// </summary>
        /// <param name="id">Chave de acesso primário da sala/grupo.</param>
        /// <returns>Autoria anexada ao lado da constituição titular dos debates abertos.</returns>
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

        /// <summary>
        /// Mostra quem faz parte da grelha de utilizadores ativos numa sala selecionada.
        /// </summary>
        /// <param name="id">Identificação interna numérico correspondente à comunidade.</param>
        /// <returns>O formato normalizado das coligações entre papéis, nome descritivo e mail exposto dos sujeitos.</returns>
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

        /// <summary>
        /// Eliminação suprema pela via do administrador, deitando a baixo qualquer regra comunitária.
        /// Destrói uma discussão (post) no ato.
        /// </summary>
        /// <param name="comunidadeId">Ambiente principal onde o tópico reside.</param>
        /// <param name="postId">O número indicial do tópico/fio da meada da discórdia/limite em causa.</param>
        /// <returns>Suprime os rascunhos em 200 OK pós-gravação de SaveChanges.</returns>
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

        /// <summary>
        /// Expulsa ativamente do catálogo da sala, removendo votos, reports, posts proativos lançados e submete uma notificação final sobre a razão do "Kick".
        /// </summary>
        /// <param name="comunidadeId">Id de base do Grupo visado.</param>
        /// <param name="utilizadorId">Indivíduo errático e passível à expulsão cívica imediata.</param>
        /// <param name="motivo">Fundamentação (geralmente obrigatória à frente da interface gráfica) explicando sumariamente o teor da decisão (Opcionalidade de BackEnd).</param>
        /// <returns>Conclusões do apagão transversal relacional da entidade vs grupo.</returns>
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

        /// <summary>
        /// Dispara o push universal "AnuncioPlataforma" iterativamente batcheado a 400 pessoas simultâneas afim de proteger e salvaguardar timeout do Contexto SQL.
        /// Apenas contas legitimamente e formalmente confirmadas recebem a diretiva global.
        /// </summary>
        /// <param name="dto">Envipeia o título formatado e corpo em texto cru.</param>
        /// <returns>Confere sucesso no balanço total das chamadas gravados pela For Loop na DB.</returns>
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

        /// <summary>
        /// Transpõe toda a biblioteca de Desafios criadas globalmente na plataforma, descarregando sem paginação.
        /// </summary>
        /// <returns>Resultados organizáveis dispostos diretamente ao painel administrativo.</returns>
        [HttpGet("desafios")]
        public async Task<IActionResult> ListarDesafios()
    {
        var list = await _context.Desafios
            .OrderByDescending(d => d.DataInicio)
            .ToListAsync();
        return Ok(list);
    }

        /// <summary>
        /// Cria novos desafios, introduzindo-os à listagem de jogos que possam ser ganhos pela comunidade.
        /// </summary>
        /// <param name="body">O corpo da proposta com todas as faculdades base: XP ganho, Requisitos e Nome textual.</param>
        /// <returns>Sucesso pós inserção local.</returns>
        [HttpPost("desafios")]
        public async Task<IActionResult> CriarDesafio([FromBody] Desafio body)
    {
        body.Id = 0;
        _context.Desafios.Add(body);
        await _context.SaveChangesAsync();
        return Ok(body);
    }

        /// <summary>
        /// Atualiza estruturalmente e fisicamente as definições e datas de atividade inerentes a um pré-fabricado desafio de XP.
        /// </summary>
        /// <param name="id">Id da tabela de controlo Desafios.</param>
        /// <param name="body">Estado intermédio refeito para sobressair os velhos pontos de paragem.</param>
        /// <returns>O descritivo do registo que acabou de surtir efeito nos relatorios.</returns>
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

        /// <summary>
        /// Limpeza do desafio, acabando irreversivelmente à listagem daquele quiz.
        /// </summary>
        /// <param name="id">Número subjacente de matrícula interna.</param>
        /// <returns>Mensagem em JSON denotando conformidade terminada.</returns>
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

        /// <summary>
        /// Mostra todo o tipo de distintivos ou troféus conquistáveis.
        /// </summary>
        /// <returns>Coleção das medalhas.</returns>
        [HttpGet("medalhas")]
        public async Task<IActionResult> ListarMedalhas()
    {
        var list = await _context.Medalhas.AsNoTracking().OrderBy(m => m.Nome).ToListAsync();
        return Ok(list);
    }

        /// <summary>
        /// Modificação em formato flexível dos valores estáticos duma badge (ícone, nomeação ou critério fundamental subjacente de XP ou tempo).
        /// </summary>
        /// <param name="id">Número representativo do Troféu singular.</param>
        /// <param name="dto">Campos modificados a substituir pontualmente.</param>
        /// <returns>As alterações espelhadas do objeto modelizador de Medalhas salvo no contexto DB.</returns>
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

    /// <summary>
    /// Data Transfer Object usado pontualmente para edição direta de aspetos superficiais do cidadão.
    /// </summary>
    public class AdminAtualizarUtilizadorDto
    {
        public string? Nome { get; set; }
        public string? Sobrenome { get; set; }
    }

    /// <summary>
    /// Corpo transmitido quando se lança um evento central em push com a premissa de anúncio comunitário.
    /// </summary>
    public class AdminNotificacaoGlobalDto
    {
        public string Titulo { get; set; } = "";
        public string Mensagem { get; set; } = "";
    }

    /// <summary>
    /// Classe leve encapsuladora dos campos dinâmicos que modelam recompensas lúdicas ou medalhas alteráveis baseados nos méritos de contagem.
    /// </summary>
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
