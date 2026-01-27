using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services;

public class MovieService : IMovieService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FilmAholicDbContext _context;
    private readonly ILogger<MovieService> _logger;
    private readonly IConfiguration _configuration;

    private readonly string _tmdbApiKey;
    private readonly string _tmdbBaseUrl = "https://api.themoviedb.org/3";
    private readonly string _omdbBaseUrl = "https://www.omdbapi.com";
    private readonly string _omdbApiKey;

    public MovieService(
        IHttpClientFactory httpClientFactory,
        FilmAholicDbContext context,
        ILogger<MovieService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _configuration = configuration;

        _tmdbApiKey = _configuration["ExternalApis:TmdbApiKey"] ?? "";
        _omdbApiKey = _configuration["ExternalApis:OmdbApiKey"] ?? "";

        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Movie search functionality will be limited.");
        }

        if (string.IsNullOrEmpty(_omdbApiKey))
        {
            _logger.LogWarning("OMDb API key is not configured. Additional movie details will be limited.");
        }
    }

    public async Task<TmdbSearchResponse> SearchMoviesAsync(string query, int page = 1)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            throw new InvalidOperationException("TMDb API key is not configured.");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"{_tmdbBaseUrl}/search/movie?api_key={_tmdbApiKey}&query={encodedQuery}&page={page}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            return result ?? new TmdbSearchResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching movies from TMDb: {Query}", query);
            throw;
        }
    }

    public async Task<TmdbMovieDto?> GetMovieDetailsFromTmdbAsync(int tmdbId)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            throw new InvalidOperationException("TMDb API key is not configured.");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_tmdbBaseUrl}/movie/{tmdbId}?api_key={_tmdbApiKey}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDb API returned status {StatusCode} for movie {TmdbId}", response.StatusCode, tmdbId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbMovieDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie details from TMDb: {TmdbId}", tmdbId);
            return null;
        }
    }

    public async Task<OmdbMovieDto?> GetMovieDetailsFromOmdbAsync(string imdbId)
    {
        if (string.IsNullOrEmpty(_omdbApiKey))
        {
            _logger.LogWarning("OMDb API key not configured. Skipping OMDb request for {ImdbId}", imdbId);
            return null;
        }

        if (string.IsNullOrEmpty(imdbId))
        {
            return null;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_omdbBaseUrl}/?apikey={_omdbApiKey}&i={Uri.EscapeDataString(imdbId)}&plot=full";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OMDb API returned status {StatusCode} for IMDb ID {ImdbId}", response.StatusCode, imdbId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OmdbMovieDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            // Check if OMDb returned an error
            if (result != null && result.Response == "False" && !string.IsNullOrEmpty(result.Error))
            {
                _logger.LogWarning("OMDb API returned error: {Error} for IMDb ID {ImdbId}", result.Error, imdbId);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting movie details from OMDb: {ImdbId}", imdbId);
            return null;
        }
    }

    public async Task<Filme?> GetMovieInfoAsync(int tmdbId)
    {
        var tmdbMovie = await GetMovieDetailsFromTmdbAsync(tmdbId);
        if (tmdbMovie == null)
        {
            return null;
        }

        OmdbMovieDto? omdbMovie = null;
        if (!string.IsNullOrEmpty(tmdbMovie.ImdbId))
        {
            omdbMovie = await GetMovieDetailsFromOmdbAsync(tmdbMovie.ImdbId);
        }

        return MapToFilme(tmdbMovie, omdbMovie);
    }

    public async Task<Filme> GetOrCreateMovieFromTmdbAsync(int tmdbId)
    {
        // Check if movie already exists in database
        var existingMovie = await _context.Set<Filme>()
            .FirstOrDefaultAsync(f => f.TmdbId == tmdbId.ToString());

        if (existingMovie != null)
        {
            return existingMovie;
        }

        // Get movie info from APIs
        var movieInfo = await GetMovieInfoAsync(tmdbId);
        if (movieInfo == null)
        {
            throw new InvalidOperationException($"Could not retrieve movie information for TMDb ID: {tmdbId}");
        }

        // Ensure TmdbId is set
        movieInfo.TmdbId = tmdbId.ToString();

        // Add to database
        _context.Set<Filme>().Add(movieInfo);
        await _context.SaveChangesAsync();

        return movieInfo;
    }

    public async Task<Filme?> UpdateMovieFromApisAsync(int filmeId)
    {
        var filme = await _context.Set<Filme>().FindAsync(filmeId);
        if (filme == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(filme.TmdbId) || !int.TryParse(filme.TmdbId, out var tmdbId))
        {
            _logger.LogWarning("Cannot update movie {FilmeId}: TMDb ID is missing or invalid", filmeId);
            return filme;
        }

        var updatedInfo = await GetMovieInfoAsync(tmdbId);
        if (updatedInfo == null)
        {
            _logger.LogWarning("Could not retrieve updated information for movie {FilmeId}", filmeId);
            return filme;
        }

        // Update movie properties
        filme.Titulo = updatedInfo.Titulo;
        filme.Genero = updatedInfo.Genero;
        filme.PosterUrl = updatedInfo.PosterUrl;
        filme.Duracao = updatedInfo.Duracao;
        filme.TmdbId = updatedInfo.TmdbId;

        await _context.SaveChangesAsync();

        return filme;
    }

    private Filme MapToFilme(TmdbMovieDto tmdbMovie, OmdbMovieDto? omdbMovie)
    {
        var filme = new Filme
        {
            TmdbId = tmdbMovie.Id.ToString(),
            Titulo = tmdbMovie.Title,
            PosterUrl = tmdbMovie.PosterPath != null
                ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}"
                : "",
            Duracao = tmdbMovie.Runtime ?? 0
        };

        // Map genres from TMDb
        if (tmdbMovie.Genres != null && tmdbMovie.Genres.Any())
        {
            filme.Genero = string.Join(", ", tmdbMovie.Genres.Select(g => g.Name));
        }
        else if (omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Genre))
        {
            filme.Genero = omdbMovie.Genre;
        }
        else
        {
            filme.Genero = "Unknown";
        }

        // Use OMDb runtime if TMDb doesn't have it
        if (filme.Duracao == 0 && omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Runtime))
        {
            var runtimeStr = omdbMovie.Runtime.Replace(" min", "").Trim();
            if (int.TryParse(runtimeStr, out var runtime))
            {
                filme.Duracao = runtime;
            }
        }

        // Use OMDb poster if TMDb doesn't have one
        if (string.IsNullOrEmpty(filme.PosterUrl) && omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Poster) && omdbMovie.Poster != "N/A")
        {
            filme.PosterUrl = omdbMovie.Poster;
        }

        return filme;
    }

    public async Task<List<Filme>> GetPopularMoviesAsync(int page = 1, int count = 20)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch popular movies.");
            return new List<Filme>();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_tmdbBaseUrl}/movie/popular?api_key={_tmdbApiKey}&page={page}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result == null || result.Results == null || !result.Results.Any())
            {
                return new List<Filme>();
            }

            var movies = new List<Filme>();
            var moviesToProcess = result.Results.Take(count).ToList();

            foreach (var tmdbMovie in moviesToProcess)
            {
                try
                {
                    var fullDetails = await GetMovieDetailsFromTmdbAsync(tmdbMovie.Id);
                    if (fullDetails == null)
                    {
                        var basicMovie = new Filme
                        {
                            TmdbId = tmdbMovie.Id.ToString(),
                            Titulo = tmdbMovie.Title,
                            PosterUrl = tmdbMovie.PosterPath != null
                                ? $"https://image.tmdb.org/t/p/w500{tmdbMovie.PosterPath}"
                                : "",
                            Duracao = 0,
                            Genero = "Unknown"
                        };
                        movies.Add(basicMovie);
                        continue;
                    }

                    // Get OMDb details if available
                    OmdbMovieDto? omdbMovie = null;
                    if (!string.IsNullOrEmpty(fullDetails.ImdbId))
                    {
                        omdbMovie = await GetMovieDetailsFromOmdbAsync(fullDetails.ImdbId);
                    }

                    var filme = MapToFilme(fullDetails, omdbMovie);
                    movies.Add(filme);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing movie {TmdbId} from popular list", tmdbMovie.Id);
                    // Continue with next movie
                }
            }

            return movies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular movies from TMDb");
            return new List<Filme>();
        }
    }
}
