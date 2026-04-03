using FilmAholic.Server.Data;
using Microsoft.Extensions.Options;

namespace FilmAholic.Server.Services;

/// Verifica periodicamente se filmes na lista "Quero Ver" já estrearam / ficaram em streaming e cria notificações.
public sealed class QueroVerEstreiaService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueroVerEstreiaService> _logger;
    private readonly QueroVerEstreiaOptions _options;

    public QueroVerEstreiaService(
        IServiceScopeFactory scopeFactory,
        IOptions<QueroVerEstreiaOptions> options,
        ILogger<QueroVerEstreiaService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("QueroVerEstreiaService disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Clamp(_options.IntervalMinutes, 1, 1440));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<FilmAholicDbContext>();
                var movieService = scope.ServiceProvider.GetRequiredService<IMovieService>();
                await QueroVerEstreiaNotifier.RunForAllUsersAsync(db, movieService, _logger, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QueroVerEstreiaService failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
