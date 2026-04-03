using FilmAholic.Server.Data;
using Microsoft.Extensions.Options;

namespace FilmAholic.Server.Services;

public sealed class ReminderJogoOptions
{
    public bool Enabled { get; set; } = true;
    public int HourUtc { get; set; } = 0;
    public int MinuteUtc { get; set; } = 0;
}

public sealed class ReminderJogoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderJogoService> _logger;
    private readonly ReminderJogoOptions _options;

    public ReminderJogoService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReminderJogoOptions> options,
        ILogger<ReminderJogoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ReminderJogoService disabled.");
            return;
        }

        await RunOnceSafe(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRunUtc(_options.HourUtc, _options.MinuteUtc);
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunOnceSafe(stoppingToken);
        }
    }

    private async Task RunOnceSafe(CancellationToken ct)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmAholicDbContext>();
            await ReminderJogoGenerator.RunFullCycleAsync(db, _logger, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReminderJogoService failed.");
        }
    }

    internal static TimeSpan DelayUntilNextRunUtc(int hourUtc, int minuteUtc)
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day,
            Math.Clamp(hourUtc, 0, 23), Math.Clamp(minuteUtc, 0, 59), 0, DateTimeKind.Utc);
        if (now >= next) next = next.AddDays(1);
        return next - now;
    }
}