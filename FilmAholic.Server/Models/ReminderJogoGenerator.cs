using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FilmAholic.Server.Services;

public static class ReminderJogoGenerator
{
    public const string TipoReminderJogo = "ReminderJogo";
    private static readonly TimeSpan InactividadeMinima = TimeSpan.FromDays(7);
    private static readonly TimeSpan IntervaloEntreNotificacoes = TimeSpan.FromDays(7);

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

            db.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = uid,
                FilmeId = null,
                Tipo = TipoReminderJogo,
                Corpo = Mensagens[Rng.Next(Mensagens.Length)],
                CriadaEm = nowUtc
            });
        }

        await db.SaveChangesAsync(ct);
        logger?.LogInformation("ReminderJogoGenerator: cycle complete at {Now}", nowUtc);
    }

    private static readonly string[] Mensagens =
[
    "Desafia-te e tenta chegar ao topo da Leaderboard! 🎮",
    "Já faz alguns dias que não jogas... O teu lugar no ranking está em risco! 👀",
    "Os teus rivais estão a subir no ranking. Estás à espera de quê? 🏆",
    "Um novo desafio aguarda-te no Higher or Lower. Consegues bater o teu recorde? 🎯",
    "A Leaderboard não se conquista a descansar! Volta ao jogo! 💪",
    "Sentes falta da adrenalina do Higher or Lower? Nós também sentimos a tua falta! 🎬",
    "Ainda dás para o torcer? Prova isso no Higher or Lower! 😏",
    "O jogo está à tua espera. Quanto tempo consegues aguentar? ⏱️",
    "Hoje pode ser o dia em que bates o teu recorde! Vai lá tentar! 🚀",
    "A tua posição na Leaderboard depende de ti. Não deixes escapar! 🔥"
];
    private static readonly Random Rng = new();
}