using FilmAholic.Server.DTOs;

namespace FilmAholic.Server.Services;

public interface ICinemaScraperService
{
    Task<List<CinemaMovieDto>> ScrapeAllAsync(CancellationToken ct = default);
}
