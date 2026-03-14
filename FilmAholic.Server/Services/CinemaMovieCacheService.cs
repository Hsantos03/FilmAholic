using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services;

public class CinemaMovieCacheService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CinemaMovieCacheService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public CinemaMovieCacheService(
        IServiceScopeFactory scopeFactory,
        ILogger<CinemaMovieCacheService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await TryRefreshIfNeeded(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextMidnight = DateTime.Today.AddDays(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("CinemaMovieCacheService: próxima atualização em {Delay}", delay);
            await Task.Delay(delay, stoppingToken);

            await TryRefreshIfNeeded(stoppingToken);
        }
    }

    private async Task TryRefreshIfNeeded(CancellationToken ct)
    {
        if (!await _lock.WaitAsync(0)) 
        {
            _logger.LogInformation("CinemaMovieCacheService: scraping já em curso, a saltar.");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FilmAholicDbContext>();

            var today = DateTime.UtcNow.Date;
            var hasToday = await db.CinemaMovieCache
                .AnyAsync(x => x.DataCache >= today, ct);

            if (hasToday)
            {
                _logger.LogInformation("CinemaMovieCacheService: cache já atualizada hoje, a saltar.");
                return;
            }

            _logger.LogInformation("CinemaMovieCacheService: a iniciar scraping…");
            await RefreshCache(scope, db, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CinemaMovieCacheService: erro ao atualizar cache.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RefreshCache(IServiceScope scope, FilmAholicDbContext db, CancellationToken ct)
    {
        var cinemaService = scope.ServiceProvider.GetRequiredService<ICinemaScraperService>();

        var movies = await cinemaService.ScrapeAllAsync(ct);

        if (!movies.Any())
        {
            _logger.LogWarning("CinemaMovieCacheService: scraping não devolveu filmes.");
            return;
        }

        await db.CinemaMovieCache.ExecuteDeleteAsync(ct);

        var entities = movies.Select(m => new CinemaMovieCache
        {
            MovieId = m.Id,
            Titulo = m.Titulo,
            Poster = m.Poster,
            Cinema = m.Cinema,
            HorariosJson = System.Text.Json.JsonSerializer.Serialize(m.Horarios),
            Genero = m.Genero,
            Duracao = m.Duracao,
            Classificacao = m.Classificacao,
            Idioma = m.Idioma,
            Sala = m.Sala,
            Link = m.Link,
            DataCache = DateTime.UtcNow
        }).ToList();

        db.CinemaMovieCache.AddRange(entities);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation("CinemaMovieCacheService: {Count} filmes guardados.", entities.Count);
    }
}
