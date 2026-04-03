using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAholic.Server.Services;

/// Pré-carrega (warm-up) os endpoints TMDB de “upcoming” numa janela de tempo.
/// Isto reduz a primeira resposta do menu de notificações após abrir o site.
public class TmdbUpcomingPreloadService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TmdbUpcomingPreloadService> _logger;

    public TmdbUpcomingPreloadService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<TmdbUpcomingPreloadService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("TmdbPreload:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("TmdbUpcomingPreloadService disabled by configuration.");
            return;
        }

        var hour = _configuration.GetValue<int>("TmdbPreload:HourUtc", 3);
        var minute = _configuration.GetValue<int>("TmdbPreload:MinuteUtc", 0);
        var maxPagesToScan = _configuration.GetValue<int>("TmdbPreload:MaxPagesToScan", 12);
        var warmStartPage = _configuration.GetValue<int>("TmdbPreload:StartPage", 1);
        var warmCount = _configuration.GetValue<int>("TmdbPreload:WarmCount", 40);

        // Executa uma vez imediatamente após start (para efeito rápido).
        await WarmOnce(stoppingToken, warmStartPage, warmCount, maxPagesToScan);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var next = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, hour, minute, 0, DateTimeKind.Utc);
            if (next <= nowUtc) next = next.AddDays(1);

            var delay = next - nowUtc;
            _logger.LogInformation("Tmdb preload next run at {NextUtc} (in {Delay}).", next, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;
                await WarmOnce(stoppingToken, warmStartPage, warmCount, maxPagesToScan);
            }
            catch (TaskCanceledException)
            {
                // Normal na shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during tmdb preload warm-up.");
            }
        }
    }

    private async Task WarmOnce(CancellationToken stoppingToken, int startPage, int warmCount, int maxPagesToScan)
    {
        if (stoppingToken.IsCancellationRequested) return;

        var todayUtc = DateTime.UtcNow.Date;
        _logger.LogInformation("Tmdb preload warm-up starting for {StartPage}/{WarmCount} (minReleaseDateUtc={TodayUtc}).", startPage, warmCount, todayUtc);

        // IServiceScopeFactory evita injectar um serviço scoped (DbContext via IMovieService)
        // dentro de um BackgroundService singleton.
        using var scope = _serviceScopeFactory.CreateScope();
        var movieService = scope.ServiceProvider.GetRequiredService<IMovieService>();

        // O menu de notificações usa: GetUpcomingMoviesAccumulatedAsync(page=1,count=40, ...).
        await movieService.GetUpcomingMoviesAccumulatedAsync(startPage, warmCount, todayUtc, maxPagesToScan);

        // Também aquece a versão paginada (fallback).
        await movieService.GetUpcomingMoviesAsync(1, warmCount);

        _logger.LogInformation("Tmdb preload warm-up finished.");
    }
}

