using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

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

            if (result?.Results == null || result.Results.Count == 0)
                return result ?? new TmdbSearchResponse();

            // Filter out adult movies right away
            result.Results = result.Results.Where(r => !r.Adult).ToList();

            // TMDb search does not return runtime; fetch details for each movie to get duration (with limited concurrency)
            var semaphore = new SemaphoreSlim(5);
            var tasks = result.Results.Select(async (movie) =>
            {
                if (movie.Runtime.HasValue && movie.Runtime.Value > 0) return;
                await semaphore.WaitAsync();
                try
                {
                    var details = await GetMovieDetailsFromTmdbAsync(movie.Id);
                    if (details?.Runtime.HasValue == true)
                        movie.Runtime = details.Runtime;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch runtime for TMDb movie {Id}", movie.Id);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            await Task.WhenAll(tasks);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching movies from TMDb: {Query}", query);
            throw;
        }
    }

    public async Task<TmdbMovieDto?> GetMovieDetailsFromTmdbAsync(int tmdbId, string language = "pt-PT")
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
        // Get Portuguese version for synopsis and genres
        var tmdbMoviePt = await GetMovieDetailsFromTmdbAsync(tmdbId);
        if (tmdbMoviePt == null)
        {
            return null;
        }

        // Exclude adult movies
        if (tmdbMoviePt.Adult)
        {
            _logger.LogInformation("Skipping TMDb movie {TmdbId} because it is flagged as adult.", tmdbId);
            return null;
        }

        // Get English version for title only
        var httpClient = _httpClientFactory.CreateClient();
        var urlEn = $"{_tmdbBaseUrl}/movie/{tmdbId}?api_key={_tmdbApiKey}&language=en-US";
        var responseEn = await httpClient.GetAsync(urlEn);
        if (!responseEn.IsSuccessStatusCode)
        {
            _logger.LogWarning("TMDb API returned status {StatusCode} for movie {TmdbId}", responseEn.StatusCode, tmdbId);
            return null;
        }

        var jsonEn = await responseEn.Content.ReadAsStringAsync();
        var tmdbMovieEn = JsonSerializer.Deserialize<TmdbMovieDto>(jsonEn, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        });

        if (tmdbMovieEn == null)
        {
            return null;
        }

        // Additional guard: english record flagged adult
        if (tmdbMovieEn.Adult)
        {
            _logger.LogInformation("Skipping TMDb movie {TmdbId} because English entry is flagged as adult.", tmdbId);
            return null;
        }

        OmdbMovieDto? omdbMovie = null;
        if (!string.IsNullOrEmpty(tmdbMoviePt.ImdbId))
        {
            omdbMovie = await GetMovieDetailsFromOmdbAsync(tmdbMoviePt.ImdbId);
        }

        return MapToFilme(tmdbMovieEn, tmdbMoviePt, omdbMovie);
    }

    public async Task<Filme> GetOrCreateMovieFromTmdbAsync(int tmdbId)
    {
        var existingMovie = await _context.Set<Filme>()
            .FirstOrDefaultAsync(f => f.TmdbId == tmdbId.ToString());

        if (existingMovie != null)
        {
            return existingMovie;
        }

        var movieInfo = await GetMovieInfoAsync(tmdbId);
        if (movieInfo == null)
        {
            throw new InvalidOperationException($"Could not retrieve movie information for TMDb ID: {tmdbId}");
        }

        movieInfo.TmdbId = tmdbId.ToString();

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

        filme.Titulo = updatedInfo.Titulo;
        filme.Genero = updatedInfo.Genero;
        filme.PosterUrl = updatedInfo.PosterUrl;
        filme.Duracao = updatedInfo.Duracao;
        filme.TmdbId = updatedInfo.TmdbId;
        filme.Ano = updatedInfo.Ano;
        filme.ReleaseDate = updatedInfo.ReleaseDate;

        await _context.SaveChangesAsync();

        return filme;
    }

    private Filme MapToFilme(TmdbMovieDto tmdbMovieEn, TmdbMovieDto tmdbMoviePt, OmdbMovieDto? omdbMovie)
    {
        var filme = new Filme
        {
            TmdbId = tmdbMovieEn.Id.ToString(),
            Titulo = !string.IsNullOrEmpty(tmdbMoviePt.Title) ? tmdbMoviePt.Title : tmdbMovieEn.Title,
            PosterUrl = tmdbMoviePt.PosterPath != null
            ? $"https://image.tmdb.org/t/p/w500{tmdbMoviePt.PosterPath}"
            : tmdbMovieEn.PosterPath != null
                ? $"https://image.tmdb.org/t/p/w500{tmdbMovieEn.PosterPath}"
                : "",
            Duracao = tmdbMovieEn.Runtime ?? 0
        };

        if (tmdbMoviePt.Genres != null && tmdbMoviePt.Genres.Any())
        {
            filme.Genero = string.Join(", ", tmdbMoviePt.Genres.Select(g => g.Name));
        }
        else if (omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Genre))
        {
            filme.Genero = omdbMovie.Genre;
        }
        else
        {
            filme.Genero = "Unknown";
        }

        if (filme.Duracao == 0 && omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Runtime))
        {
            var runtimeStr = omdbMovie.Runtime.Replace(" min", "").Trim();
            if (int.TryParse(runtimeStr, out var runtime))
            {
                filme.Duracao = runtime;
            }
        }

        if (string.IsNullOrEmpty(filme.PosterUrl) && omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Poster) && omdbMovie.Poster != "N/A")
        {
            filme.PosterUrl = omdbMovie.Poster;
        }

        // set Ano from TMDb release date (year)
        if (!string.IsNullOrEmpty(tmdbMoviePt.ReleaseDate) && tmdbMoviePt.ReleaseDate.Length >= 4 && int.TryParse(tmdbMoviePt.ReleaseDate.Substring(0, 4), out var ano))
        {
            filme.Ano = ano;
        }
        if (filme.Ano == null && omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Year) && int.TryParse(omdbMovie.Year.Trim(), out var anoOmdb))
        {
            filme.Ano = anoOmdb;
        }

        // New: parse and store full release date when available (TMDb format is "yyyy-MM-dd")
        if (!string.IsNullOrEmpty(tmdbMoviePt.ReleaseDate))
        {
            if (DateTime.TryParseExact(tmdbMoviePt.ReleaseDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var parsedDate))
            {
                // store as UTC (DB will persist DateTime)
                filme.ReleaseDate = parsedDate;
            }
            else if (DateTime.TryParse(tmdbMoviePt.ReleaseDate, out var fallbackDate))
            {
                filme.ReleaseDate = fallbackDate;
            }
        }
        else if (omdbMovie != null && !string.IsNullOrEmpty(omdbMovie.Released) && DateTime.TryParse(omdbMovie.Released, out var omdbDate))
        {
            filme.ReleaseDate = omdbDate;
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

            // Filter out adult movies
            var moviesToProcess = result.Results.Where(m => !m.Adult).Take(count).ToList();

            var movies = new List<Filme>();

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

                    OmdbMovieDto? omdbMovie = null;
                    if (!string.IsNullOrEmpty(fullDetails.ImdbId))
                    {
                        omdbMovie = await GetMovieDetailsFromOmdbAsync(fullDetails.ImdbId);
                    }

                    var filme = MapToFilme(tmdbMovie, fullDetails, omdbMovie);
                    movies.Add(filme);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing movie {TmdbId} from popular list", tmdbMovie.Id);
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

    public async Task<List<PopularActorDto>> GetPopularActorsAsync(int page = 1, int count = 20)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch popular actors.");
            return new List<PopularActorDto>();
        }

        try
        {
            var allPeople = new List<TmdbPersonDto>();
            var pagesToFetch = Math.Min(5, (int)Math.Ceiling(count / 20.0) + 2);

            for (int p = 1; p <= pagesToFetch; p++)
            {
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"{_tmdbBaseUrl}/person/popular?api_key={_tmdbApiKey}&page={p}&language=en-US";
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) break;

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TmdbPopularPeopleResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                });

                if (result?.Results == null || result.Results.Count == 0) break;
                allPeople.AddRange(result.Results);
            }

            if (allPeople.Count == 0)
            {
                return new List<PopularActorDto>();
            }

            var rng = new Random();
            var shuffled = allPeople
                .Where(p => !string.IsNullOrEmpty(p.ProfilePath))
                .OrderBy(_ => rng.Next())
                .ToList();

            var actors = shuffled
                .Take(count)
                .Select(p => new PopularActorDto
                {
                    Id = p.Id,
                    Nome = p.Name,
                    Popularidade = p.Popularity,
                    FotoUrl = $"https://image.tmdb.org/t/p/w500{p.ProfilePath}"
                })
                .ToList();

            if (actors.Count > count)
            {
                actors = actors.Take(count).ToList();
            }

            return actors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular actors from TMDb");
            return new List<PopularActorDto>();
        }
    }

    public async Task<RatingsDto> GetRatingsAsync(string? tmdbId, string? title)
    {
        var dto = new RatingsDto();

        int? parsedTmdbId = null;

        if (!string.IsNullOrWhiteSpace(tmdbId) && int.TryParse(tmdbId, out var tmp))
            parsedTmdbId = tmp;

        if (parsedTmdbId == null && !string.IsNullOrWhiteSpace(title))
        {
            try
            {
                var search = await SearchMoviesAsync(title, 1);
                var first = search?.Results?.FirstOrDefault();
                if (first != null)
                    parsedTmdbId = first.Id;
            }
            catch
            {
            }
        }

        TmdbMovieDto? tmdbMovie = null;

        if (parsedTmdbId != null)
        {
            tmdbMovie = await GetMovieDetailsFromTmdbAsync(parsedTmdbId.Value);
            if (tmdbMovie != null)
            {
                dto.TmdbVoteAverage = tmdbMovie.VoteAverage;
                dto.TmdbVoteCount = tmdbMovie.VoteCount;
                dto.ImdbId = tmdbMovie.ImdbId;
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.ImdbId))
        {
            var omdb = await GetMovieDetailsFromOmdbAsync(dto.ImdbId);
            if (omdb != null)
            {
                dto.ImdbRating = string.IsNullOrWhiteSpace(omdb.ImdbRating) ? null : omdb.ImdbRating;
                dto.Metascore = string.IsNullOrWhiteSpace(omdb.Metascore) ? null : omdb.Metascore;

                if (omdb.Ratings != null && omdb.Ratings.Count > 0)
                {
                    var rt = omdb.Ratings.FirstOrDefault(r =>
                        !string.IsNullOrWhiteSpace(r.Source) &&
                        r.Source.ToLower().Contains("rotten tomatoes"));

                    if (rt != null && !string.IsNullOrWhiteSpace(rt.Value))
                        dto.RottenTomatoes = rt.Value;
                }
            }
        }

        return dto;
    }

    public async Task<List<Filme>> GetRecommendationsAsync(int tmdbId, int count = 10)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch recommendations.");
            return new List<Filme>();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var url = $"{_tmdbBaseUrl}/movie/{tmdbId}/recommendations?api_key={_tmdbApiKey}&language=pt-PT&page=1";

            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDb recommendations returned status {StatusCode} for movie {TmdbId}", response.StatusCode, tmdbId);
                return new List<Filme>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result?.Results == null || result.Results.Count == 0)
            {
                url = $"{_tmdbBaseUrl}/movie/{tmdbId}/similar?api_key={_tmdbApiKey}&language=pt-PT&page=1";
                response = await httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    json = await response.Content.ReadAsStringAsync();
                    result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = false
                    });
                }
            }

            if (result?.Results == null || result.Results.Count == 0)
            {
                return new List<Filme>();
            }

            // Filter out adult movies
            var recommendations = new List<Filme>();
            var moviesToProcess = result.Results.Where(m => !m.Adult).Take(count).ToList();

            foreach (var tmdbMovie in moviesToProcess)
            {
                try
                {
                    var filme = await GetOrCreateMovieFromTmdbAsync(tmdbMovie.Id);
                    recommendations.Add(filme);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing recommendation movie {TmdbId}", tmdbMovie.Id);
                }
            }

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching recommendations from TMDb for movie {TmdbId}", tmdbId);
            return new List<Filme>();
        }
    }

    public async Task<List<ActorSearchResultDto>> SearchActorsAsync(string query)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot search actors.");
            return new List<ActorSearchResultDto>();
        }

        if (string.IsNullOrWhiteSpace(query))
            return new List<ActorSearchResultDto>();

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var encodedQuery = Uri.EscapeDataString(query.Trim());
            var url = $"{_tmdbBaseUrl}/search/person?api_key={_tmdbApiKey}&query={encodedQuery}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchPersonResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result?.Results == null || result.Results.Count == 0)
                return new List<ActorSearchResultDto>();

            return result.Results
                .Select(p => new ActorSearchResultDto
                {
                    Id = p.Id,
                    Nome = p.Name,
                    FotoUrl = string.IsNullOrEmpty(p.ProfilePath)
                        ? ""
                        : $"https://image.tmdb.org/t/p/w185{p.ProfilePath}"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching actors for query {Query}", query);
            return new List<ActorSearchResultDto>();
        }
    }

    public async Task<ActorDetailsDto?> GetActorDetailsAsync(int personId)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch actor details.");
            return null;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_tmdbBaseUrl}/person/{personId}?api_key={_tmdbApiKey}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            var details = JsonSerializer.Deserialize<TmdbPersonDetailsDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (details == null) return null;

            return new ActorDetailsDto
            {
                Id = details.Id,
                Nome = details.Name,
                FotoUrl = string.IsNullOrEmpty(details.ProfilePath) ? null : $"https://image.tmdb.org/t/p/w500{details.ProfilePath}",
                Biografia = details.Biography,
                DataNascimento = details.Birthday,
                LocalNascimento = details.PlaceOfBirth,
                Departamento = details.KnownForDepartment,
                DataFalecimento = details.Deathday
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching actor details for person {PersonId}", personId);
            return null;
        }
    }

    public async Task<List<ActorMovieDto>> GetMoviesByActorAsync(int personId)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch movies by actor.");
            return new List<ActorMovieDto>();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_tmdbBaseUrl}/person/{personId}/movie_credits?api_key={_tmdbApiKey}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbPersonMovieCreditsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result?.Cast == null || result.Cast.Count == 0)
                return new List<ActorMovieDto>();

            return result.Cast
                .OrderByDescending(m => m.ReleaseDate)
                .Select(m => new ActorMovieDto
                {
                    Id = m.Id,
                    Titulo = m.Title,
                    PosterUrl = string.IsNullOrEmpty(m.PosterPath)
                        ? null
                        : $"https://image.tmdb.org/t/p/w500{m.PosterPath}",
                    Personagem = m.Character,
                    DataLancamento = m.ReleaseDate
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching movies for person {PersonId}", personId);
            return new List<ActorMovieDto>();
        }
    }

    public async Task<List<CastMemberDto>> GetCastAsync(int tmdbId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/credits?api_key={_tmdbApiKey}&language=pt-PT";
            var response = await httpClient.GetFromJsonAsync<TmdbPopularPeopleResponse>(url);

            return (response?.Cast ?? new())
                .OrderBy(c => c.Order)
                .Take(15)
                .Select(c => new CastMemberDto
                {
                    Id = c.Id,
                    Nome = c.Name,
                    Personagem = c.Character,
                    FotoUrl = string.IsNullOrEmpty(c.ProfilePath)
                                    ? null
                                    : $"https://image.tmdb.org/t/p/w185{c.ProfilePath}"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetCastAsync error: {ex.Message}");
            return new List<CastMemberDto>();
        }
    }
}
