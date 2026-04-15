using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;

namespace FilmAholic.Server.Services;

/// <summary>
/// Serviço responsável por gerenciar filmes e informações relacionadas.
/// </summary>
public class MovieService : IMovieService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FilmAholicDbContext _context;
    private readonly ILogger<MovieService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    private readonly string _tmdbApiKey;
    private readonly string _tmdbBaseUrl = "https://api.themoviedb.org/3";
    private readonly string _omdbBaseUrl = "https://www.omdbapi.com";
    private readonly string _omdbApiKey;

    /// <summary>
    /// Serviço responsável por gerenciar filmes e informações relacionadas.
    /// </summary>
    public MovieService(
        IHttpClientFactory httpClientFactory,
        FilmAholicDbContext context,
        ILogger<MovieService> logger,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;

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


    /// <summary>
    /// Pesquisa filmes no TMDb com base na query fornecida.
    /// </summary>
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


    /// <summary>
    /// Obtém os detalhes de um filme do TMDb.
    /// </summary>
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


    /// <summary>
    /// Obtém os detalhes de um filme do OMDb.
    /// </summary>
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
    
    /// <summary>
    /// Obtém as informações de um filme, combinando dados do TMDb e OMDb.
    /// </summary>
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
    
    /// <summary>
    /// Obtém ou cria um filme a partir do TMDb.
    /// </summary>
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
    
    /// <summary>
    /// Atualiza as informações de um filme a partir das APIs.
    /// </summary>
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

        // Persist ratings during update
        filme.ImdbRating = updatedInfo.ImdbRating;
        filme.Metascore = updatedInfo.Metascore;
        filme.RottenTomatoes = updatedInfo.RottenTomatoes;
        filme.LastRatingsUpdate = updatedInfo.LastRatingsUpdate;

        await _context.SaveChangesAsync();

        return filme;
    }

    /// <summary>
    /// Mapeia os dados do TMDb e OMDb para um objeto Filme.
    /// </summary>
    private Filme MapToFilme(TmdbMovieDto tmdbMovieEn, TmdbMovieDto tmdbMoviePt, OmdbMovieDto? omdbMovie)
    {
        var posterUrl = TmdbPosterW500Url(tmdbMoviePt.PosterPath);
        if (string.IsNullOrEmpty(posterUrl))
            posterUrl = TmdbPosterW500Url(tmdbMovieEn.PosterPath);

        // Runtime: o 1.º argumento costuma ser o item da lista (upcoming/popular) sem "runtime";
        // o 2.º é o filme completo pt-PT de /movie/{id}, onde o TMDB devolve runtime em minutos.
        var runtimeMin = tmdbMoviePt.Runtime ?? tmdbMovieEn.Runtime;
        var filme = new Filme
        {
            TmdbId = tmdbMovieEn.Id.ToString(),
            Titulo = !string.IsNullOrEmpty(tmdbMoviePt.Title) ? tmdbMoviePt.Title : tmdbMovieEn.Title,
            PosterUrl = posterUrl,
            Duracao = runtimeMin ?? 0
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

        if (tmdbMoviePt.Genres != null && tmdbMoviePt.Genres.Count > 0)
            filme.TmdbGenreIds = tmdbMoviePt.Genres.Select(g => g.Id).Distinct().ToList();
        else if (tmdbMovieEn.GenreIds is { Count: > 0 })
            filme.TmdbGenreIds = tmdbMovieEn.GenreIds.Distinct().ToList();

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

        // Ratings persistence
        if (omdbMovie != null)
        {
            filme.ImdbRating = string.IsNullOrWhiteSpace(omdbMovie.ImdbRating) ? null : omdbMovie.ImdbRating;
            filme.Metascore = string.IsNullOrWhiteSpace(omdbMovie.Metascore) ? null : omdbMovie.Metascore;

            if (omdbMovie.Ratings != null && omdbMovie.Ratings.Count > 0)
            {
                var rt = omdbMovie.Ratings.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.Source) &&
                    r.Source.ToLower().Contains("rotten tomatoes"));

                if (rt != null && !string.IsNullOrWhiteSpace(rt.Value))
                    filme.RottenTomatoes = rt.Value;
            }
            filme.LastRatingsUpdate = DateTime.UtcNow;
        }

        return filme;
    }

    /// <summary>
    /// Obtém os filmes populares do TMDb.
    /// </summary>
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

            if (result?.Results == null || !result.Results.Any())
                return new List<Filme>();

            return await HydrateTmdbResultsToFilmesAsync(result.Results, count, "popular list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular movies from TMDb");
            return new List<Filme>();
        }
    }

    /// <summary>
    /// Gera a chave de cache para filmes populares com mínimo de votos.
    /// </summary>
    private static string PopularMinVotesCacheKey(int count, int minVoteCount, int maxPages) =>
        $"tmdb-popular-minvotes:{count}:{minVoteCount}:{maxPages}";

    /// <summary>
    /// Obtém os filmes populares do TMDb com mínimo de votos.
    /// </summary>
    public async Task<List<Filme>> GetPopularMoviesWithMinVotesAsync(int count = 10, int minVoteCount = 500, int maxPages = 5)
    {
        var cacheKey = PopularMinVotesCacheKey(count, minVoteCount, maxPages);
        if (_cache.TryGetValue(cacheKey, out List<Filme>? cached) && cached != null)
            return cached;

        var list = await FetchPopularMoviesWithMinVotesUncachedAsync(count, minVoteCount, maxPages, CancellationToken.None);
        var cacheHours = Math.Max(1, _configuration.GetValue<int>("HomepageFeatured:CacheHours", 24));
        _cache.Set(cacheKey, list, TimeSpan.FromHours(cacheHours));
        return list;
    }

    /// <summary>
    /// Atualiza o cache de filmes em destaque na página inicial.
    /// </summary>
    public async Task RefreshHomepageFeaturedCacheAsync(CancellationToken cancellationToken = default)
    {
        var count = Math.Clamp(_configuration.GetValue<int>("HomepageFeatured:Count", 10), 1, 40);
        var minRatings = Math.Max(1, _configuration.GetValue<int>("HomepageFeatured:MinRatings", 500));
        var maxPages = Math.Clamp(_configuration.GetValue<int>("HomepageFeatured:MaxPages", 10), 1, 20);
        var cacheKey = PopularMinVotesCacheKey(count, minRatings, maxPages);
        var cacheHours = Math.Max(1, _configuration.GetValue<int>("HomepageFeatured:CacheHours", 24));

        var list = await FetchPopularMoviesWithMinVotesUncachedAsync(count, minRatings, maxPages, cancellationToken);
        _cache.Set(cacheKey, list, TimeSpan.FromHours(cacheHours));
        _logger.LogInformation("Homepage featured cache refreshed ({Count} filmes, chave {Key}).", list.Count, cacheKey);
    }

    /// <summary>
    /// Obtém os filmes populares do TMDb com mínimo de votos.
    /// </summary>
    private async Task<List<Filme>> FetchPopularMoviesWithMinVotesUncachedAsync(int count, int minVoteCount, int maxPages, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch popular movies.");
            return new List<Filme>();
        }

        var result = new List<Filme>();
        var seenTmdbIds = new HashSet<string>(StringComparer.Ordinal);
        var page = 1;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            while (result.Count < count && page <= maxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var url = $"{_tmdbBaseUrl}/movie/popular?api_key={_tmdbApiKey}&page={page}&language=pt-PT";

                var response = await httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var searchResult = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = false
                });

                if (searchResult?.Results == null || !searchResult.Results.Any())
                    break;

                // Filtrar filmes com vote_count >= minVoteCount e não adultos
                var filtered = searchResult.Results
                    .Where(m => !m.Adult && m.VoteCount >= minVoteCount)
                    .ToList();

                if (filtered.Any())
                {
                    // Hidratar filmes filtrados
                    var hydrated = await HydrateTmdbResultsToFilmesAsync(filtered, filtered.Count, "popular with min votes");

                    foreach (var filme in hydrated)
                    {
                        if (!string.IsNullOrEmpty(filme.TmdbId) && seenTmdbIds.Add(filme.TmdbId))
                        {
                            result.Add(filme);
                            if (result.Count >= count)
                                break;
                        }
                    }
                }

                page++;

                // Se não houver mais páginas no TMDB, parar
                if (page > searchResult.TotalPages)
                    break;
            }

            return result.Take(count).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error fetching popular movies with min votes from TMDb");
            return result;
        }
    }

    /// <summary>
    /// Obtém os filmes em breve do TMDb.
    /// </summary>
    public async Task<List<Filme>> GetUpcomingMoviesAsync(int page = 1, int count = 20)
    {
        var cacheHours = Math.Max(1, _configuration.GetValue<int>("TmdbUpcomingCacheHours", 24));
        var todayUtc = DateTime.UtcNow.Date;
        var cacheKey = $"tmdb-upcoming:{page}:{count}:{todayUtc:yyyy-MM-dd}";

        if (_cache.TryGetValue(cacheKey, out List<Filme>? cached) && cached != null)
            return cached;

        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch upcoming movies.");
            return new List<Filme>();
        }

        try
        {
            var result = await FetchTmdbUpcomingPageAsync(page);
            if (result?.Results == null || !result.Results.Any())
                return new List<Filme>();

            // Hydrate completo (PT para géneros/sinopse + EN para título, e OMDb quando disponível).
            var hydrated = await HydrateTmdbResultsToFilmesAsync(result.Results, count, "upcoming list");

            _cache.Set(cacheKey, hydrated, TimeSpan.FromHours(cacheHours));
            return hydrated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching upcoming movies from TMDb");
            return new List<Filme>();
        }
    }
    
    /// <summary>
    /// Obtém os filmes em breve acumulados do TMDb.
    /// </summary>
    public async Task<List<Filme>> GetUpcomingMoviesAccumulatedAsync(
        int startPage,
        int desiredCount,
        DateTime minReleaseDateUtc,
        int maxPagesToScan = 12)
    {
        var cacheHours = Math.Max(1, _configuration.GetValue<int>("TmdbUpcomingCacheHours", 24));
        var minDay = minReleaseDateUtc.Date;
        var cacheKey = $"tmdb-upcoming-acc:{startPage}:{desiredCount}:{minDay:yyyy-MM-dd}:{maxPagesToScan}";

        if (_cache.TryGetValue(cacheKey, out List<Filme>? cached) && cached != null)
            return cached;

        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch upcoming movies.");
            return new List<Filme>();
        }

        if (startPage < 1) startPage = 1;
        if (desiredCount < 1) return new List<Filme>();

        maxPagesToScan = Math.Clamp(maxPagesToScan, 1, 30);
        var aggregated = new List<Filme>();
        var seenTmdb = new HashSet<string>(StringComparer.Ordinal);
        var pagesScanned = 0;
        var page = startPage;

        try
        {
            while (aggregated.Count < desiredCount && pagesScanned < maxPagesToScan)
            {
                var resp = await FetchTmdbUpcomingPageAsync(page);
                if (resp?.Results == null || resp.Results.Count == 0)
                    break;

                var passed = resp.Results
                    .Where(m => !m.Adult)
                    .Where(m => TmdbListReleaseOnOrAfter(m.ReleaseDate, minDay))
                    .OrderBy(m => ParseTmdbListReleaseDateOrMax(m.ReleaseDate))
                    .ToList();

                if (passed.Count > 0)
                {
                    var need = desiredCount - aggregated.Count;
                    var takeForHydrate = Math.Min(passed.Count, need + 8);
                    var hydrated = await HydrateTmdbResultsToFilmesAsync(passed, takeForHydrate, "upcoming accumulated");

                    foreach (var f in hydrated)
                    {
                        if (string.IsNullOrEmpty(f.TmdbId) || !seenTmdb.Add(f.TmdbId)) continue;
                        if (!FilmeReleaseOnOrAfter(f, minDay)) continue;

                        aggregated.Add(f);
                        if (aggregated.Count >= desiredCount)
                            break;
                    }
                }

                pagesScanned++;
                if (resp.TotalPages > 0 && page >= resp.TotalPages)
                    break;

                page++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accumulating upcoming movies from TMDb");
        }

        var result = aggregated
            .OrderBy(f => f.ReleaseDate ?? new DateTime(f.Ano ?? (minDay.Year + 10), 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .Take(desiredCount)
            .ToList();

        _cache.Set(cacheKey, result, TimeSpan.FromHours(cacheHours));
        return result;
    }

    /// <summary>
    /// Obtém os filmes em breve do TMDb.
    /// </summary>
    private async Task<TmdbSearchResponse?> FetchTmdbUpcomingPageAsync(int page)
    {
        var httpClient = _httpClientFactory.CreateClient();
        // Sem region fixo: com region=PT o catálogo/ordem do TMDB muda bastante (outros títulos, Mario pode sumir).
        var url =
            $"{_tmdbBaseUrl}/movie/upcoming?api_key={_tmdbApiKey}&page={page}&language=pt-PT";

        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = false
        });
    }

    /// <summary>
    /// Verifica se a data de lançamento do filme é igual ou posterior a uma data mínima.
    /// </summary>
    private static bool TmdbListReleaseOnOrAfter(string? releaseDate, DateTime minDayUtc)
    {
        if (!TryParseTmdbDateOnly(releaseDate, out var d))
            return false;

        return d.Date >= minDayUtc.Date;
    }

    /// <summary>
    /// Obtém a data de lançamento do filme ou o valor máximo se não estiver disponível.
    /// </summary>
    private static DateTime ParseTmdbListReleaseDateOrMax(string? releaseDate)
    {
        if (TryParseTmdbDateOnly(releaseDate, out var d))
            return d.Date;

        return DateTime.MaxValue;
    }

    /// <summary>
    /// Tenta analisar a data de lançamento do TMDb.
    /// </summary>
    private static bool TryParseTmdbDateOnly(string? releaseDate, out DateTime date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(releaseDate))
            return false;

        return DateTime.TryParseExact(
            releaseDate.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// Verifica se a data de lançamento do filme é igual ou posterior a uma data mínima.
    /// </summary>
    private static bool FilmeReleaseOnOrAfter(Filme f, DateTime minDayUtc)
    {
        if (f.ReleaseDate.HasValue)
            return f.ReleaseDate.Value.Date >= minDayUtc.Date;

        if (f.Ano.HasValue)
            return f.Ano.Value >= minDayUtc.Year;

        return false;
    }

    /// <summary>
    /// Obtém a URL do poster do TMDb com largura de 500 pixels.
    /// </summary>
    private static string TmdbPosterW500Url(string? posterPath)
    {
        if (string.IsNullOrWhiteSpace(posterPath))
            return "";

        var p = posterPath.Trim();
        if (!p.StartsWith('/'))
            p = "/" + p;

        return $"https://image.tmdb.org/t/p/w500{p}";
    }

    /// <summary>
    /// Obtém a lista de filmes mais bem avaliados.
    /// </summary>
    public async Task<List<Filme>> GetTopRatedMoviesAsync(int page = 1, int count = 20)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch top rated movies.");
            return new List<Filme>();
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var url = $"{_tmdbBaseUrl}/movie/top_rated?api_key={_tmdbApiKey}&page={page}&language=pt-PT";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result?.Results == null || !result.Results.Any())
                return new List<Filme>();

            return await HydrateTmdbResultsToFilmesAsync(result.Results, count, "top_rated list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching top rated movies from TMDb");
            return new List<Filme>();
        }
    }

    /// <summary>
    /// Obtém a lista de filmes clássicos.
    /// </summary>
    public async Task<List<Filme>> GetClassicDiscoverMoviesAsync(int page = 1, int count = 20, string? primaryReleaseDateLte = null, int minVoteCount = 500)
    {
        if (string.IsNullOrEmpty(_tmdbApiKey))
        {
            _logger.LogWarning("TMDb API key is not configured. Cannot fetch discover movies.");
            return new List<Filme>();
        }

        var lte = string.IsNullOrWhiteSpace(primaryReleaseDateLte) ? "1999-12-31" : primaryReleaseDateLte.Trim();
        if (minVoteCount < 0) minVoteCount = 0;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var sortBy = Uri.EscapeDataString("vote_average.desc");
            var url =
                $"{_tmdbBaseUrl}/discover/movie?api_key={_tmdbApiKey}&language=pt-PT&page={page}" +
                $"&primary_release_date.lte={Uri.EscapeDataString(lte)}" +
                $"&sort_by={sortBy}" +
                $"&vote_count.gte={minVoteCount}" +
                "&include_adult=false";

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TmdbSearchResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            });

            if (result?.Results == null || !result.Results.Any())
                return new List<Filme>();

            return await HydrateTmdbResultsToFilmesAsync(result.Results, count, "discover/classic list");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching discover (classic) movies from TMDb");
            return new List<Filme>();
        }
    }

    /// <summary>
    /// Hidrata resultados TMDB (lista) para <see cref="Filme"/> com detalhes pt-PT + OMDb quando possível.
    /// Utiliza paralelismo para acelerar o processo.
    /// </summary>
    private async Task<List<Filme>> HydrateTmdbResultsToFilmesAsync(IReadOnlyList<TmdbMovieDto> results, int count, string sourceLabel)
    {
        var moviesToProcess = results.Where(m => !m.Adult).Take(count).ToList();
        
        // Pré-carregar IDs existentes para evitar múltiplas queries pequenas
        var tmdbIds = moviesToProcess.Select(m => m.Id.ToString()).ToList();
        var existingMovies = await _context.Set<Filme>()
            .Where(f => tmdbIds.Contains(f.TmdbId))
            .AsNoTracking()
            .ToListAsync();

        var tasks = moviesToProcess.Select(async tmdbMovie =>
        {
            try
            {
                Filme? movieToAdd = null;
                var existing = existingMovies.FirstOrDefault(e => e.TmdbId == tmdbMovie.Id.ToString());

                var fullDetails = await GetMovieDetailsFromTmdbAsync(tmdbMovie.Id);
                if (fullDetails == null)
                {
                    movieToAdd = new Filme
                    {
                        TmdbId = tmdbMovie.Id.ToString(),
                        Titulo = tmdbMovie.Title,
                        PosterUrl = TmdbPosterW500Url(tmdbMovie.PosterPath),
                        Duracao = 0,
                        Genero = "Unknown",
                        TmdbGenreIds = tmdbMovie.GenreIds is { Count: > 0 }
                            ? tmdbMovie.GenreIds.Distinct().ToList()
                            : new List<int>()
                    };
                }
                else
                {
                    OmdbMovieDto? omdbMovie = null;
                    if (!string.IsNullOrEmpty(fullDetails.ImdbId))
                        omdbMovie = await GetMovieDetailsFromOmdbAsync(fullDetails.ImdbId);

                    movieToAdd = MapToFilme(tmdbMovie, fullDetails, omdbMovie);
                }

                if (movieToAdd != null && existing != null)
                {
                    movieToAdd.Id = existing.Id;
                }

                return movieToAdd;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error processing movie {TmdbId} from {Source}", tmdbMovie.Id, sourceLabel);
                return null;
            }
        });

        var resultsList = await Task.WhenAll(tasks);
        return resultsList.Where(m => m != null).Cast<Filme>().ToList();
    }

    /// <summary>
    /// Obtém os atores mais populares.
    /// </summary>
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

            // Ordem estável por popularidade (TMDb). Evitar shuffle aleatório — cada pedido devolvia outro “top”.
            var actors = allPeople
                .Where(p => !string.IsNullOrEmpty(p.ProfilePath))
                .DistinctBy(p => p.Id)
                .OrderByDescending(p => p.Popularity)
                .Take(count)
                .Select(p => new PopularActorDto
                {
                    Id = p.Id,
                    Nome = p.Name,
                    Popularidade = p.Popularity,
                    FotoUrl = $"https://image.tmdb.org/t/p/w500{p.ProfilePath}"
                })
                .ToList();

            return actors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching popular actors from TMDb");
            return new List<PopularActorDto>();
        }
    }

    /// <summary>
    /// Obtém a classificação do utilizador para um filme específico.
    /// </summary>
    public async Task<RatingsDto> GetRatingsAsync(string? tmdbId, string? title)
    {
        var dto = new RatingsDto();

        // 1. Try to find in DB first
        Models.Filme? dbFilme = null;
        if (!string.IsNullOrWhiteSpace(tmdbId))
        {
            dbFilme = await _context.Set<Models.Filme>()
                .FirstOrDefaultAsync(f => f.TmdbId == tmdbId);
        }
        else if (!string.IsNullOrWhiteSpace(title))
        {
            dbFilme = await _context.Set<Models.Filme>()
                .FirstOrDefaultAsync(f => f.Titulo.ToLower() == title.ToLower());
        }

        // If found and fresh (e.g. 24h), use it
        if (dbFilme != null && dbFilme.LastRatingsUpdate.HasValue &&
            (DateTime.UtcNow - dbFilme.LastRatingsUpdate.Value).TotalHours < 24)
        {
            dto.ImdbRating = dbFilme.ImdbRating;
            dto.Metascore = dbFilme.Metascore;
            dto.RottenTomatoes = dbFilme.RottenTomatoes;
            dto.ImdbId = dbFilme.TmdbId; // Might be a misnomer in RatingsDto if it expects real IMDb id, but let's check TMDb vote too

            // We still need TMDb real-time votes if possible, but for now let's skip the expensive TMDB call if we have fresh base data
            // Or better, fetch TMDb separately if we want live counts.
        }

        int? parsedTmdbId = null;
        if (!string.IsNullOrWhiteSpace(tmdbId) && int.TryParse(tmdbId, out var tmp))
            parsedTmdbId = tmp;

        if (parsedTmdbId == null && dbFilme != null && int.TryParse(dbFilme.TmdbId, out var tmp2))
            parsedTmdbId = tmp2;

        if (parsedTmdbId == null && !string.IsNullOrWhiteSpace(title))
        {
            try
            {
                var search = await SearchMoviesAsync(title, 1);
                var first = search?.Results?.FirstOrDefault();
                if (first != null)
                    parsedTmdbId = first.Id;
            }
            catch { }
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

        // 3. fetch from OMDb if data is missing or stale
        if (string.IsNullOrEmpty(dto.ImdbRating) && !string.IsNullOrWhiteSpace(dto.ImdbId))
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

                // Update DB with these new ratings
                if (dbFilme != null)
                {
                    dbFilme.ImdbRating = dto.ImdbRating;
                    dbFilme.Metascore = dto.Metascore;
                    dbFilme.RottenTomatoes = dto.RottenTomatoes;
                    dbFilme.LastRatingsUpdate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
        }

        return dto;
    }

    /// <summary>
    /// Obtém recomendações de filmes com base em um filme específico.
    /// </summary>
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

    /// <summary>
    /// Obtém os atores mais populares com base em uma consulta.
    /// </summary>
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
                        : $"https://image.tmdb.org/t/p/w185{p.ProfilePath}",
                    Popularidade = p.Popularity
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching actors for query {Query}", query);
            return new List<ActorSearchResultDto>();
        }
    }

    /// <summary>
    /// Obtém os detalhes de um ator com base no seu ID.
    /// </summary>
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

    /// <summary>
    /// Obtém os filmes de um ator com base no seu ID.
    /// </summary>
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

    /// <summary>
    /// Obtém os membros do elenco de um filme com base no seu ID.
    /// </summary>
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


    /// <summary>
    /// Verifica se um filme está disponível em streaming.
    /// </summary>
    public async Task<bool> IsAvailableInStreamingAsync(int tmdbId)
    {
        var apiKey = _configuration["ExternalApis:TmdbApiKey"];
        var client = _httpClientFactory.CreateClient();

        var url = $"https://api.themoviedb.org/3/movie/{tmdbId}/watch/providers?api_key={apiKey}";
        var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<JsonElement>(json);

        if (!data.TryGetProperty("results", out var results))
            return false;

        if (results.TryGetProperty("PT", out var pt))
        {
            if (pt.TryGetProperty("flatrate", out var streaming))
            {
                return streaming.GetArrayLength() > 0;
            }
        }
        return false;
    }
}
