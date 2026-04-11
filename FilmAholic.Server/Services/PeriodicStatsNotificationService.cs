using FilmAholic.Server.Data;
using Microsoft.Extensions.Options;

namespace FilmAholic.Server.Services;

/// FR70: agenda geração periódica de resumos de estatísticas.
public sealed class PeriodicStatsNotificationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PeriodicStatsNotificationService> _logger;
    private readonly PeriodicStatsNotificationOptions _options;

    public PeriodicStatsNotificationService(
        IServiceScopeFactory scopeFactory,
        IOptions<PeriodicStatsNotificationOptions> options,
        ILogger<PeriodicStatsNotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

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
            var delay = BackgroundServiceScheduling.DelayUntilNextRunUtc(_options.HourUtc, _options.MinuteUtc);
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

}
