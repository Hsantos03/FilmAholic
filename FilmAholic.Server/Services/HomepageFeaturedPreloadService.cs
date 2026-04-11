using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAholic.Server.Services;

/// <summary>
/// Pré-carrega a lista “Filmes em Destaque” da landing (api/filmes/populares-comunidade) para memória.
/// Arranque imediato + repetição diária à hora UTC configurada (predefinição: 00:00 UTC ≈ meia-noite em UTC).
/// </summary>
public class HomepageFeaturedPreloadService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomepageFeaturedPreloadService> _logger;

    public HomepageFeaturedPreloadService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<HomepageFeaturedPreloadService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _configuration.GetValue<bool>("HomepageFeatured:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("HomepageFeaturedPreloadService disabled by configuration.");
            return;
        }

        var hour = _configuration.GetValue<int>("HomepageFeatured:HourUtc", 0);
        var minute = _configuration.GetValue<int>("HomepageFeatured:MinuteUtc", 0);
        hour = Math.Clamp(hour, 0, 23);
        minute = Math.Clamp(minute, 0, 59);

        await WarmOnce(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowUtc = DateTime.UtcNow;
            var next = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, hour, minute, 0, DateTimeKind.Utc);
            if (next <= nowUtc)
                next = next.AddDays(1);

            var delay = next - nowUtc;
            _logger.LogInformation("Homepage featured preload next run at {NextUtc} UTC (in {Delay}).", next, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
                if (stoppingToken.IsCancellationRequested)
                    break;
                await WarmOnce(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during homepage featured preload.");
            }
        }
    }

    private async Task WarmOnce(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        _logger.LogInformation("Homepage featured preload starting.");
        using var scope = _serviceScopeFactory.CreateScope();
        var movieService = scope.ServiceProvider.GetRequiredService<IMovieService>();
        await movieService.RefreshHomepageFeaturedCacheAsync(stoppingToken);
        _logger.LogInformation("Homepage featured preload finished.");
    }
}
