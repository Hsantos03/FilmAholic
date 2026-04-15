using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FilmAholic.Server.Services;

/// <summary>
/// Gera notificações de lembrete para o jogo Higher or Lower.
/// </summary>
public static class ReminderJogoGenerator
{
    public const string TipoReminderJogo = "ReminderJogo";
    private static readonly TimeSpan InactividadeMinima = TimeSpan.FromDays(7);
    private static readonly TimeSpan IntervaloEntreNotificacoes = TimeSpan.FromDays(7);


    /// <summary>
    /// Executa um ciclo completo de geração de notificações ReminderJogo.
    /// </summary>
    public static async Task RunFullCycleAsync(
        FilmAholicDbContext db,
        ILogger? logger,
        CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var cutoff = nowUtc - InactividadeMinima;

        // Todos os utilizadores que NÃO jogaram nos últimos 7 dias
        var utilizadoresAtivos = await db.Users
            .Select(u => u.Id)
            .ToListAsync(ct);

        var jogadoresRecentes = await db.GameHistories
            .Where(g => g.DataCriacao >= cutoff)
            .Select(g => g.UtilizadorId)
            .Distinct()
            .ToListAsync(ct);

        var utilizadoresComNotifAtiva = await db.PreferenciasNotificacao
            .AsNoTracking()
            .Where(p => p.ReminderJogoAtiva)
            .Select(p => p.UtilizadorId)
            .ToListAsync(ct);

        var elegíveis = utilizadoresAtivos
            .Where(uid => !jogadoresRecentes.Contains(uid)
                       && utilizadoresComNotifAtiva.Contains(uid))
            .ToList();

        foreach (var uid in elegíveis)
        {
            ct.ThrowIfCancellationRequested();

            // Máximo 1 notificação a cada 7 dias por utilizador
            var ultimaNotif = await db.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == uid && n.Tipo == TipoReminderJogo)
                .OrderByDescending(n => n.CriadaEm)
                .Select(n => (DateTime?)n.CriadaEm)
                .FirstOrDefaultAsync(ct);

            if (ultimaNotif.HasValue && nowUtc - ultimaNotif.Value < IntervaloEntreNotificacoes)
                continue;

            var ix = Rng.Next(ReminderJogoMensagens.TextosSemEmoji.Length);
            db.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = uid,
                FilmeId = null,
                Tipo = TipoReminderJogo,
                Corpo = ReminderJogoCorpoJson.Serialize(ix, ReminderJogoMensagens.TextosSemEmoji[ix]),
                CriadaEm = nowUtc
            });
        }

        await db.SaveChangesAsync(ct);
        logger?.LogInformation("ReminderJogoGenerator: cycle complete at {Now}", nowUtc);
    }

    /// <summary>
    /// Garante uma notificação HoL quando elegível (mesmas regras que o ciclo diário),
    /// para não depender só do job em background após login/registo.
    /// </summary>
    public static async Task EnsureForUserIfEligibleAsync(
        FilmAholicDbContext db,
        string utilizadorId,
        ILogger? logger,
        CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var cutoff = nowUtc - InactividadeMinima;

        var userExists = await db.Users.AnyAsync(u => u.Id == utilizadorId, ct);
        if (!userExists) return;

        var prefs = await db.PreferenciasNotificacao
            .FirstOrDefaultAsync(p => p.UtilizadorId == utilizadorId, ct);
        if (prefs == null)
        {
            prefs = new PreferenciasNotificacao
            {
                UtilizadorId = utilizadorId,
                NovaEstreiaAtiva = true,
                NovaEstreiaFrequencia = "Diaria",
                ResumoEstatisticasAtiva = true,
                ResumoEstatisticasFrequencia = "Semanal",
                AtualizadaEm = nowUtc
            };
            db.PreferenciasNotificacao.Add(prefs);
            await db.SaveChangesAsync(ct);
        }

        if (!prefs.ReminderJogoAtiva) return;

        var jogouRecente = await db.GameHistories
            .AnyAsync(g => g.UtilizadorId == utilizadorId && g.DataCriacao >= cutoff, ct);
        if (jogouRecente) return;

        var ultimaNotif = await db.Notificacoes
            .AsNoTracking()
            .Where(n => n.UtilizadorId == utilizadorId && n.Tipo == TipoReminderJogo)
            .OrderByDescending(n => n.CriadaEm)
            .Select(n => (DateTime?)n.CriadaEm)
            .FirstOrDefaultAsync(ct);

        if (ultimaNotif.HasValue && nowUtc - ultimaNotif.Value < IntervaloEntreNotificacoes)
            return;

        var ix = Rng.Next(ReminderJogoMensagens.TextosSemEmoji.Length);
        db.Notificacoes.Add(new Notificacao
        {
            UtilizadorId = utilizadorId,
            FilmeId = null,
            Tipo = TipoReminderJogo,
            Corpo = ReminderJogoCorpoJson.Serialize(ix, ReminderJogoMensagens.TextosSemEmoji[ix]),
            CriadaEm = nowUtc
        });
        await db.SaveChangesAsync(ct);
        logger?.LogDebug("ReminderJogoGenerator: ensured reminder for user {UserId}", utilizadorId);
    }

    /// <summary>
    /// Gera números aleatórios para seleção de mensagens.
    /// </summary>
    private static readonly Random Rng = new();
}
