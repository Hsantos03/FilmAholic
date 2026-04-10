using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Services;

public interface IMovieService
{
    Task<TmdbSearchResponse> SearchMoviesAsync(string query, int page = 1);

    Task<TmdbMovieDto?> GetMovieDetailsFromTmdbAsync(int tmdbId, string language = "en-US");

    Task<OmdbMovieDto?> GetMovieDetailsFromOmdbAsync(string imdbId);

    Task<Filme?> GetMovieInfoAsync(int tmdbId);

    Task<Filme> GetOrCreateMovieFromTmdbAsync(int tmdbId);

    Task<Filme?> UpdateMovieFromApisAsync(int filmeId);

    Task<List<Filme>> GetPopularMoviesAsync(int page = 1, int count = 20);

    /// TMDB /movie/popular com filtro de mínimo de votos (vote_count).
    Task<List<Filme>> GetPopularMoviesWithMinVotesAsync(int count = 10, int minVoteCount = 500, int maxPages = 5);

    /// TMDB /movie/upcoming — usado para preencher “novas estreias” quando a BD não tem filmes com releaseDate futuro.
    Task<List<Filme>> GetUpcomingMoviesAsync(int page = 1, int count = 20);

    /// Percorre várias páginas do TMDB
    /// Útil porque uma página costuma misturar estreias já ocorridas em algumas regiões.
    Task<List<Filme>> GetUpcomingMoviesAccumulatedAsync(int startPage, int desiredCount, DateTime minReleaseDateUtc, int maxPagesToScan = 12);

    /// TMDB /movie/top_rated — mistura clássicos e filmes muito bem votados.
    Task<List<Filme>> GetTopRatedMoviesAsync(int page = 1, int count = 20);

    /// TMDB /discover/movie com filtros tipo “clássicos” (data limite, nota, mín. votos).
    Task<List<Filme>> GetClassicDiscoverMoviesAsync(int page = 1, int count = 20, string? primaryReleaseDateLte = null, int minVoteCount = 500);

    Task<List<PopularActorDto>> GetPopularActorsAsync(int page = 1, int count = 20);

    Task<List<ActorSearchResultDto>> SearchActorsAsync(string query);

    Task<ActorDetailsDto?> GetActorDetailsAsync(int personId);

    Task<List<ActorMovieDto>> GetMoviesByActorAsync(int personId);

    Task<RatingsDto> GetRatingsAsync(string? tmdbId, string? title);

    Task<List<Filme>> GetRecommendationsAsync(int tmdbId, int count = 10);

    Task<List<CastMemberDto>> GetCastAsync(int tmdbId);

    Task<bool> IsAvailableInStreamingAsync(int tmdbId);
}
