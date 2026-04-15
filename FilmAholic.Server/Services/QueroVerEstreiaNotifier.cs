using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FilmAholic.Server.Services;

/// <summary>
/// Serviço responsável por notificar os utilizadores sobre estreias de filmes.
/// </summary>
public static class QueroVerEstreiaNotifier
{
    public const string Tipo = "FilmeDisponivel";

    private static readonly TimeZoneInfo PortugalTimeZone = CreatePortugalTimeZone();

    /// <summary>
    /// Cria a informação de fuso horário para Portugal.
    /// </summary>
    private static TimeZoneInfo CreatePortugalTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }

    /// <summary>
    /// Executa o serviço de notificações de estreias para um utilizador específico.
    /// </summary>
    public static async Task<int> RunForUserAsync(
        FilmAholicDbContext db,
        IMovieService movieService,
        string userId,
        ILogger? logger,
        CancellationToken ct)
    {
        var prefs = await db.PreferenciasNotificacao
            .FirstOrDefaultAsync(p => p.UtilizadorId == userId, ct);

        if (prefs == null)
        {
            prefs = new PreferenciasNotificacao
            {
                UtilizadorId = userId,
                NovaEstreiaAtiva = true,
                NovaEstreiaFrequencia = "Diaria",
                ResumoEstatisticasAtiva = true,
                ResumoEstatisticasFrequencia = "Semanal",
                AtualizadaEm = DateTime.UtcNow
            };
            db.PreferenciasNotificacao.Add(prefs);
            await db.SaveChangesAsync(ct);
        }

        if (!prefs.FilmeDisponivelAtiva)
            return 0;

        var nowUtc = DateTime.UtcNow;

        var queroVer = await db.UserMovies
            .AsNoTracking()
            .Where(um => um.UtilizadorId == userId && um.JaViu == false)
            .Select(um => um.FilmeId)
            .ToListAsync(ct);

        if (queroVer.Count == 0)
            return 0;

        var filmes = await db.Filmes
            .Where(f => queroVer.Contains(f.Id))
            .ToListAsync(ct);

        var added = 0;
        foreach (var filme in filmes)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(filme.TmdbId)) continue;
            if (!int.TryParse(filme.TmdbId, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var tmdbId))
                continue;

            if (!filme.ReleaseDate.HasValue)
                continue;

            var hojePortugal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, PortugalTimeZone).Date;
            var releaseDay = filme.ReleaseDate.Value.Date;

            // "Hoje" = dia civil em Portugal (Europe/Lisbon); datas de estreia já passadas não disparam cinema.
            var estreouHoje = releaseDay == hojePortugal;
            // Streaming só para estreias ainda futuras em relação a hoje em Portugal.
            var streaming =
                releaseDay > hojePortugal && await movieService.IsAvailableInStreamingAsync(tmdbId);

            if (!estreouHoje && !streaming) continue;

            var alreadyNotified = await db.Notificacoes
                .AnyAsync(n =>
                    n.UtilizadorId == userId &&
                    n.FilmeId == filme.Id &&
                    n.Tipo == Tipo, ct);
            if (alreadyNotified) continue;

            db.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = userId,
                FilmeId = filme.Id,
                Tipo = Tipo,
                Corpo = estreouHoje
                    ? $"🎬 {filme.Titulo} estreou hoje nos cinema!"
                    : $"📺 {filme.Titulo} já está disponível em streaming!",
                CriadaEm = nowUtc
            });
            added++;
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger?.LogInformation("QueroVerEstreia: {Count} notificações para utilizador {UserId}", added, userId);
        }

        return added;
    }

    /// <summary>
    /// Executa o serviço de notificações de estreias para todos os utilizadores.
    /// </summary>
    public static async Task RunForAllUsersAsync(
        FilmAholicDbContext db,
        IMovieService movieService,
        ILogger? logger,
        CancellationToken ct)
    {
        var userIds = await db.UserMovies
            .Where(um => !um.JaViu)
            .Select(um => um.UtilizadorId)
            .Distinct()
            .ToListAsync(ct);

        var total = 0;
        foreach (var uid in userIds)
        {
            ct.ThrowIfCancellationRequested();
            total += await RunForUserAsync(db, movieService, uid, logger, ct);
        }

        if (total > 0)
            logger?.LogInformation("QueroVerEstreia: ciclo global criou {Total} notificações.", total);
    }
}
