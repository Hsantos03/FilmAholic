using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services;

/// <summary>
/// Lógica partilhada entre o <see cref="PeriodicStatsNotificationService"/> e o endpoint de teste imediato.
/// </summary>
public static class PeriodicStatsNotificationGenerator
{
    public const string TipoResumo = "ResumoEstatisticas";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>
    /// Obtém o intervalo de resumo com base na frequência fornecida.
    /// </summary>
    public static TimeSpan GetResumoInterval(string? frequencia)
    {
        var f = (frequencia ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(f)) return TimeSpan.FromDays(7);
        return f.Equals("Diaria", StringComparison.OrdinalIgnoreCase)
            ? TimeSpan.FromDays(1)
            : TimeSpan.FromDays(7);
    }

    /// <summary>
    /// Obtém o filme mais visto na semana na plataforma.
    /// </summary>
    public static async Task<ResumoFilmeComunidadeDto?> GetFilmeMaisVistoSemanaPlataformaAsync(
        FilmAholicDbContext db,
        DateTime nowUtc,
        int communityWeekDays,
        CancellationToken ct)
    {
        var weekCutoff = nowUtc.Date.AddDays(-Math.Max(1, communityWeekDays));

        var topGlobal = await db.UserMovies
            .AsNoTracking()
            .Where(um => um.JaViu && um.Data >= weekCutoff)
            .GroupBy(um => um.FilmeId)
            .Select(g => new { FilmeId = g.Key, C = g.Count() })
            .OrderByDescending(x => x.C)
            .ThenBy(x => x.FilmeId)
            .FirstOrDefaultAsync(ct);

        if (topGlobal == null) return null;

        var filme = await db.Filmes.AsNoTracking().FirstOrDefaultAsync(f => f.Id == topGlobal.FilmeId, ct);
        if (filme == null) return null;

        return new ResumoFilmeComunidadeDto
        {
            FilmeId = filme.Id,
            Titulo = filme.Titulo,
            MarcacoesNaSemana = topGlobal.C
        };
    }

    /// <summary>
    /// Executa um ciclo completo de geração de notificações periódicas de estatísticas.
    /// </summary>
    public static async Task RunFullCycleAsync(
        FilmAholicDbContext db,
        PeriodicStatsNotificationOptions options,
        ILogger? logger,
        CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var recentCutoff = nowUtc.AddDays(-Math.Max(1, options.RecentActivityDays));

        var filmeComunidade = await GetFilmeMaisVistoSemanaPlataformaAsync(db, nowUtc, options.CommunityWeekDays, ct);

        var prefsRows = await db.PreferenciasNotificacao
            .AsNoTracking()
            .Where(p => p.ResumoEstatisticasAtiva)
            .Select(p => new { p.UtilizadorId, p.ResumoEstatisticasFrequencia })
            .ToListAsync(ct);

        foreach (var row in prefsRows)
        {
            ct.ThrowIfCancellationRequested();

            var hasRecent = await db.UserMovies
                .AsNoTracking()
                .AnyAsync(um => um.UtilizadorId == row.UtilizadorId && um.JaViu && um.Data >= recentCutoff, ct);
            if (!hasRecent)
                continue;

            var interval = GetResumoInterval(row.ResumoEstatisticasFrequencia);
            var lastAt = await db.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == row.UtilizadorId && n.Tipo == TipoResumo)
                .OrderByDescending(n => n.CriadaEm)
                .Select(n => (DateTime?)n.CriadaEm)
                .FirstOrDefaultAsync(ct);

            if (lastAt.HasValue && nowUtc - lastAt.Value < interval)
                continue;

            var watched = await db.UserMovies
                .AsNoTracking()
                .Include(um => um.Filme)
                .Where(um => um.UtilizadorId == row.UtilizadorId && um.JaViu)
                .ToListAsync(ct);

            var minutos = watched.Sum(um => um.Filme?.Duracao ?? 0);
            var horas = Math.Round(minutos / 60.0, 1);
            var generos = WatchStatisticsHelper.CountByIndividualGenre(watched)
                .Take(5)
                .Select(g => new ResumoGeneroContagemDto { Nome = g.genero, Filmes = g.total })
                .ToList();

            var corpo = new ResumoEstatisticasCorpoDto
            {
                TempoTotalHoras = horas,
                GenerosMaisVistos = generos,
                FilmeMaisVistoSemanaPlataforma = filmeComunidade
            };

            var json = JsonSerializer.Serialize(corpo, JsonOpts);

            db.Notificacoes.Add(new Notificacao
            {
                UtilizadorId = row.UtilizadorId,
                FilmeId = null,
                Tipo = TipoResumo,
                Corpo = json,
                CriadaEm = nowUtc
            });
        }

        await db.SaveChangesAsync(ct);
        logger?.LogInformation("PeriodicStatsNotificationGenerator: cycle complete at {Now}", nowUtc);
    }
}
