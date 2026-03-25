using FilmAholic.Server.Controllers;
using FilmAholic.Server.DTOs;
using Microsoft.Playwright;

namespace FilmAholic.Server.Services;

public class CinemaScraperService : ICinemaScraperService
{
    private readonly ILogger<CinemaScraperService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    // Args estáveis — sem --single-process que causa TargetClosedException
    private static readonly string[] BrowserArgs =
    {
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-dev-shm-usage",
        "--disable-gpu"
    };

    public CinemaScraperService(
        ILogger<CinemaScraperService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<List<CinemaMovieDto>> ScrapeAllAsync(CancellationToken ct = default)
    {
        var results = new List<CinemaMovieDto>();

        try
        {
            var nos = await ScrapeWithRetryAsync(ScrapeNosAsync, "Cinema NOS", ct);
            _logger.LogInformation("Cinema NOS: {Count} filmes", nos.Count);
            results.AddRange(nos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer scrape do Cinema NOS.");
        }

        try
        {
            var city = await ScrapeWithRetryAsync(ScrapeCinemaCityAsync, "Cinema City", ct);
            var enriched = await EnrichWithTmdb(city);
            _logger.LogInformation("Cinema City: {Count} filmes (após enrich)", enriched.Count);
            results.AddRange(enriched);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer scrape do Cinema City.");
        }

        return results;
    }

    // Tenta até 3 vezes com delay crescente
    private async Task<List<CinemaMovieDto>> ScrapeWithRetryAsync(
        Func<Task<List<CinemaMovieDto>>> scraper,
        string name,
        CancellationToken ct,
        int maxAttempts = 3)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await scraper();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = attempt * 5000;
                _logger.LogWarning("Tentativa {Attempt}/{Max} falhou para {Name}: {Msg}. A tentar em {Delay}ms.",
                    attempt, maxAttempts, name, ex.Message, delay);
                await Task.Delay(delay, ct);
            }
        }
        return new List<CinemaMovieDto>();
    }

    private async Task<List<CinemaMovieDto>> ScrapeNosAsync()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = BrowserArgs
        });

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36"
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync("https://www.cinemas.nos.pt/filmes", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45000
        });

        // Espera pelos cards ou timeout
        try
        {
            await page.WaitForSelectorAsync(".movie-card", new() { Timeout = 20000 });
        }
        catch
        {
            _logger.LogWarning("Seletor .movie-card não encontrado no NOS.");
            return new List<CinemaMovieDto>();
        }

        await page.WaitForTimeoutAsync(2000);

        var cards = await page.QuerySelectorAllAsync(".movie-card");
        var movies = new List<CinemaMovieDto>();

        for (int i = 0; i < cards.Count; i++)
        {
            try
            {
                var card = cards[i];
                var titulo = await card.EvalOnSelectorAsync<string>(".movie-card__title", "el => el.innerText") ?? "";
                var genero = await card.EvalOnSelectorAsync<string>(".movie-card__genre", "el => el.innerText") ?? "";
                var info = await card.EvalOnSelectorAsync<string>(".movie-card__info", "el => el.innerText") ?? "";
                var imgSrc = await card.EvalOnSelectorAsync<string>(".movie-card__image", "el => el.getAttribute('src')") ?? "";
                var link = await card.EvalOnSelectorAsync<string>(".movie-card__link", "el => el.getAttribute('href')") ?? "";

                var infoParts = info.Split(" - ");
                var classificacao = infoParts.Length > 0 ? infoParts[0].Trim() : "";
                var duracao = infoParts.Length > 1 ? infoParts[1].Trim() : "";

                if (!string.IsNullOrEmpty(titulo))
                {
                    movies.Add(new CinemaMovieDto
                    {
                        Titulo = titulo.Trim(),
                        Poster = imgSrc.StartsWith("//") ? "https:" + imgSrc : imgSrc,
                        Cinema = "Cinema NOS",
                        Genero = genero.Trim(),
                        Duracao = duracao,
                        Classificacao = classificacao,
                        Link = link
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro no card NOS {i}: {msg}", i, ex.Message);
            }
        }

        return movies;
    }

    private async Task<List<CinemaMovieDto>> ScrapeCinemaCityAsync()
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = BrowserArgs
        });

        await using var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36"
        });

        var page = await context.NewPageAsync();
        var movies = new List<CinemaMovieDto>();

        await page.GotoAsync("https://www.cinemacity.pt/", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45000
        });

        try
        {
            await page.WaitForSelectorAsync(".flip-container", new() { Timeout = 20000 });
        }
        catch
        {
            _logger.LogWarning("Seletor .flip-container não encontrado no Cinema City.");
            return movies;
        }

        var cards = await page.QuerySelectorAllAsync(".flip-container");

        foreach (var card in cards)
        {
            try
            {
                var titulo = await card.EvalOnSelectorAsync<string>(".title", "el => el.textContent");
                var poster = await card.EvalOnSelectorAsync<string>(".front img", "el => el.src");
                var link = await card.EvalOnSelectorAsync<string>(".front a", "el => el.href");

                var duracaoText = "";
                var duracaoEl = await card.QuerySelectorAsync(".back .text p:nth-child(4)");
                if (duracaoEl != null)
                {
                    var text = await duracaoEl.TextContentAsync();
                    var match = System.Text.RegularExpressions.Regex.Match(text ?? "", @"\d+");
                    if (match.Success)
                    {
                        var mins = int.Parse(match.Value);
                        duracaoText = $"{mins / 60}h {mins % 60}min";
                    }
                }

                var classificacao = "";
                var classEl = await card.QuerySelectorAsync(".back .text p:nth-child(3)");
                if (classEl != null)
                {
                    var text = await classEl.TextContentAsync();
                    classificacao = text?.Replace("Rating:", "").Trim() ?? "";
                }

                if (!string.IsNullOrWhiteSpace(titulo))
                {
                    movies.Add(new CinemaMovieDto
                    {
                        Titulo = titulo.Trim(),
                        Poster = poster ?? "",
                        Link = link ?? "",
                        Cinema = "Cinema City",
                        Duracao = duracaoText,
                        Classificacao = classificacao,
                        Genero = ""
                    });
                }
            }
            catch { }
        }

        return movies;
    }

    private async Task<List<CinemaMovieDto>> EnrichWithTmdb(List<CinemaMovieDto> movies)
    {
        var apiKey = _configuration["ExternalApis:TmdbApiKey"];
        var anoAtual = DateTime.Now.Year;
        var httpClient = _httpClientFactory.CreateClient();

        var suffixes = new[] {
            " - 2D ATMOS", " - 3D ATMOS", " - 2D", " - 3D",
            " 2D ATMOS", " 3D ATMOS", " 2D", " 3D",
            " ATMOS", " VP - 2D", " VP", " IMAX"
        };

        foreach (var movie in movies)
        {
            try
            {
                var tituloLimpo = movie.Titulo.Trim();
                foreach (var suffix in suffixes)
                    if (tituloLimpo.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                        tituloLimpo = tituloLimpo[..^suffix.Length].Trim();

                movie.Titulo = tituloLimpo;

                System.Text.Json.JsonElement results = default;

                foreach (var ano in new[] { anoAtual, anoAtual - 1, 0 })
                {
                    var url = ano > 0
                        ? $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(tituloLimpo)}&language=pt-PT&year={ano}"
                        : $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(tituloLimpo)}&language=pt-PT";

                    var response = await httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode) continue;

                    var json = await response.Content.ReadAsStringAsync();
                    var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    results = data.GetProperty("results");
                    if (results.GetArrayLength() > 0) break;
                }

                if (results.GetArrayLength() == 0) continue;

                var tituloNorm = tituloLimpo.ToLower().Trim();
                System.Text.Json.JsonElement best = results[0];

                for (int i = 0; i < results.GetArrayLength(); i++)
                {
                    var r = results[i];
                    var ptTitle = r.TryGetProperty("title", out var t) ? t.GetString()?.ToLower().Trim() : "";
                    var origTitle = r.TryGetProperty("original_title", out var ot) ? ot.GetString()?.ToLower().Trim() : "";
                    if (ptTitle == tituloNorm || origTitle == tituloNorm) { best = r; break; }
                }

                var tmdbId = best.GetProperty("id").GetInt32();
                var detailsResponse = await httpClient.GetAsync(
                    $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=pt-PT");

                if (!detailsResponse.IsSuccessStatusCode) continue;

                var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
                var details = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(detailsJson);

                if (details.TryGetProperty("poster_path", out var poster) && poster.GetString() != null)
                    movie.Poster = $"https://image.tmdb.org/t/p/w500{poster.GetString()}";

                if (details.TryGetProperty("runtime", out var runtime) && runtime.GetInt32() > 0)
                {
                    var mins = runtime.GetInt32();
                    movie.Duracao = $"{mins / 60}h {mins % 60}min";
                }

                if (details.TryGetProperty("genres", out var genres) && genres.GetArrayLength() > 0)
                {
                    var genreNames = new List<string>();
                    foreach (var g in genres.EnumerateArray())
                        if (g.TryGetProperty("name", out var gName))
                            genreNames.Add(gName.GetString() ?? "");
                    movie.Genero = string.Join(", ", genreNames);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro ao enriquecer {Titulo}: {msg}", movie.Titulo, ex.Message);
            }
        }

        return movies
            .GroupBy(m => m.Titulo.ToLower().Trim())
            .Select(g => g.First())
            .ToList();
    }
}