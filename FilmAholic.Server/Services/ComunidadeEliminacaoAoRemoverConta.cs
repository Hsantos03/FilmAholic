using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services;

/// <summary>
/// Quando uma conta deixa de existir: remove comunidades onde era o único admin
/// e notifica os restantes membros ativos.
/// </summary>
public static class ComunidadeEliminacaoAoRemoverConta
{
    public const string TipoNotificacao = "comunidade_eliminada";

    public static async Task ExecutarAsync(FilmAholicDbContext context, string utilizadorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(utilizadorId)) return;

        var comunidadesComoAdmin = await context.ComunidadeMembros
            .AsNoTracking()
            .Where(m => m.UtilizadorId == utilizadorId && m.Role == "Admin" && m.Status == "Ativo")
            .Select(m => m.ComunidadeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var cid in comunidadesComoAdmin)
        {
            var outrosAdmins = await context.ComunidadeMembros
                .CountAsync(
                    m => m.ComunidadeId == cid && m.UtilizadorId != utilizadorId && m.Role == "Admin" && m.Status == "Ativo",
                    cancellationToken);
            if (outrosAdmins > 0)
                continue;

            var com = await context.Comunidades.FirstOrDefaultAsync(c => c.Id == cid, cancellationToken);
            if (com == null)
                continue;

            var nome = com.Nome;
            var membrosParaAvisar = await context.ComunidadeMembros
                .AsNoTracking()
                .Where(m => m.ComunidadeId == cid && m.UtilizadorId != utilizadorId && m.Status == "Ativo")
                .Select(m => m.UtilizadorId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var antigas = context.NotificacoesComunidade.Where(n => n.ComunidadeId == cid);
            context.NotificacoesComunidade.RemoveRange(antigas);

            var corpo = JsonSerializer.Serialize(new
            {
                comunidadeNome = nome,
                mensagem = "A comunidade foi eliminada porque a conta do administrador deixou de existir na plataforma."
            });

            foreach (var uid in membrosParaAvisar)
            {
                context.NotificacoesComunidade.Add(new NotificacaoComunidade
                {
                    UtilizadorId = uid,
                    ComunidadeId = null,
                    PostId = null,
                    Tipo = TipoNotificacao,
                    Corpo = corpo,
                    CriadaEm = DateTime.UtcNow
                });
            }

            context.Comunidades.Remove(com);
        }
    }
}
