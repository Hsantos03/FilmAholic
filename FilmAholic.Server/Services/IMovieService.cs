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

    /// <summary>
    /// TMDB /movie/upcoming — usado para preencher “novas estreias” quando a BD não tem filmes com releaseDate futuro.
    /// </summary>
    Task<List<Filme>> GetUpcomingMoviesAsync(int page = 1, int count = 20);

    /// <summary>
    /// Percorre várias páginas do TMDB /movie/upcoming até obter <paramref name="desiredCount"/> filmes
    /// com data de estreia &gt;= <paramref name="minReleaseDateUtc"/> (só a data, UTC). Útil porque uma
    /// página costuma misturar estreias já ocorridas em algumas regiões.
    /// </summary>
    Task<List<Filme>> GetUpcomingMoviesAccumulatedAsync(int startPage, int desiredCount, DateTime minReleaseDateUtc, int maxPagesToScan = 12);

    /// <summary>TMDB /movie/top_rated — mistura clássicos e filmes muito bem votados.</summary>
    Task<List<Filme>> GetTopRatedMoviesAsync(int page = 1, int count = 20);

    /// <summary>TMDB /discover/movie com filtros tipo “clássicos” (data limite, nota, mín. votos).</summary>
    Task<List<Filme>> GetClassicDiscoverMoviesAsync(int page = 1, int count = 20, string? primaryReleaseDateLte = null, int minVoteCount = 500);

    Task<List<PopularActorDto>> GetPopularActorsAsync(int page = 1, int count = 20);

    Task<List<ActorSearchResultDto>> SearchActorsAsync(string query);

    Task<ActorDetailsDto?> GetActorDetailsAsync(int personId);

    Task<List<ActorMovieDto>> GetMoviesByActorAsync(int personId);

    Task<RatingsDto> GetRatingsAsync(string? tmdbId, string? title);
    Task<List<Filme>> GetRecommendationsAsync(int tmdbId, int count = 10);
    Task<List<CastMemberDto>> GetCastAsync(int tmdbId);
}
