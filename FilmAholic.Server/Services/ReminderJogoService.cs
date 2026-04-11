using FilmAholic.Server.Data;
using Microsoft.Extensions.Options;

namespace FilmAholic.Server.Services;

/// <summary>
/// Configuração do serviço de lembretes de jogos.
/// </summary>
public sealed class ReminderJogoOptions
{
    public bool Enabled { get; set; } = true;
    public int HourUtc { get; set; } = 0;
    public int MinuteUtc { get; set; } = 0;
}

/// <summary>
/// Serviço responsável por gerir os lembretes de jogos.
/// </summary>
public sealed class ReminderJogoService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderJogoService> _logger;
    private readonly ReminderJogoOptions _options;

    /// <summary>
    /// Inicializa uma nova instância do serviço de lembretes de jogos.
    /// </summary>
    public ReminderJogoService(
        IServiceScopeFactory scopeFactory,
        IOptions<ReminderJogoOptions> options,
        ILogger<ReminderJogoService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }
    
    /// <summary>
    /// Executa o serviço de lembretes de jogos.
    /// </summary>
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
            var delay = BackgroundServiceScheduling.DelayUntilNextRunUtc(_options.HourUtc, _options.MinuteUtc);
            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunOnceSafe(stoppingToken);
        }
    }

    /// <summary>
    /// Executa uma vez o ciclo completo de lembretes de jogos de forma segura.
    /// </summary>
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

    /// <summary>
    /// Calcula o tempo restante até a próxima execução do serviço de lembretes de jogos.
    /// </summary>
    internal static TimeSpan DelayUntilNextRunUtc(int hourUtc, int minuteUtc)
    {
        var now = DateTime.UtcNow;
        var next = new DateTime(now.Year, now.Month, now.Day,
            Math.Clamp(hourUtc, 0, 23), Math.Clamp(minuteUtc, 0, 59), 0, DateTimeKind.Utc);
        if (now >= next) next = next.AddDays(1);
        return next - now;
    }
}