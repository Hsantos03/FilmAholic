using FilmAholic.Server.Data;
using Microsoft.Extensions.Options;

namespace FilmAholic.Server.Services;

/// FR70: agenda geração periódica de resumos de estatísticas.
/// /// <summary>
/// Serviço responsável por gerar notificações periódicas de estatísticas.
/// </summary>
public sealed class PeriodicStatsNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PeriodicStatsNotificationService> _logger;
    private readonly PeriodicStatsNotificationOptions _options;

    /// <summary>
    /// Inicializa uma nova instância do serviço de notificações periódicas de estatísticas.
    /// </summary>
    public PeriodicStatsNotificationService(
        IServiceScopeFactory scopeFactory,
        IOptions<PeriodicStatsNotificationOptions> options,
        ILogger<PeriodicStatsNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Executa o serviço de notificações periódicas de estatísticas.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("PeriodicStatsNotificationService disabled.");
            return;
        }

        await RunOnceSafe(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRunUtc(_options.HourUtc, _options.MinuteUtc);
            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunOnceSafe(stoppingToken);
        }
    }

    /// <summary>
    /// Executa um ciclo seguro de geração de notificações periódicas de estatísticas.
    /// </summary>
    private async Task RunOnceSafe(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmAholicDbContext>();
            await PeriodicStatsNotificationGenerator.RunFullCycleAsync(db, _options, _logger, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PeriodicStatsNotificationService failed.");
        }
    }

    /// <summary>
    /// Obtém o atraso até a próxima execução do serviço em UTC.
    /// </summary>
    internal static TimeSpan DelayUntilNextRunUtc(int hourUtc, int minuteUtc)
    {
        var now = DateTime.UtcNow;
        var h = Math.Clamp(hourUtc, 0, 23);
        var m = Math.Clamp(minuteUtc, 0, 59);
        var next = new DateTime(now.Year, now.Month, now.Day, h, m, 0, DateTimeKind.Utc);
        if (now >= next)
            next = next.AddDays(1);
        return next - now;
    }
}
