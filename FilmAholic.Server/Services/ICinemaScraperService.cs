using FilmAholic.Server.DTOs;

namespace FilmAholic.Server.Services;

/// <summary>
/// Serviço responsável por obter informações de filmes em cinemas.
/// </summary>
public interface ICinemaScraperService
{
    Task<List<CinemaMovieDto>> ScrapeAllAsync(CancellationToken ct = default);
}
