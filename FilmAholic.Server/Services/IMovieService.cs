using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Services;

public interface IMovieService
{
    Task<TmdbSearchResponse> SearchMoviesAsync(string query, int page = 1);

    Task<TmdbMovieDto?> GetMovieDetailsFromTmdbAsync(int tmdbId);

    Task<OmdbMovieDto?> GetMovieDetailsFromOmdbAsync(string imdbId);

    Task<Filme?> GetMovieInfoAsync(int tmdbId);

    Task<Filme> GetOrCreateMovieFromTmdbAsync(int tmdbId);

    Task<Filme?> UpdateMovieFromApisAsync(int filmeId);

    Task<List<Filme>> GetPopularMoviesAsync(int page = 1, int count = 20);

    Task<List<PopularActorDto>> GetPopularActorsAsync(int page = 1, int count = 10);

    Task<RatingsDto> GetRatingsAsync(string? tmdbId, string? title);
    Task<List<Filme>> GetRecommendationsAsync(int tmdbId, int count = 10);
}
