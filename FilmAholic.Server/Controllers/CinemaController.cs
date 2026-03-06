using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using System.Text.Json;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CinemaController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public CinemaController(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }    

        [HttpGet("em-cartaz")]
        public async Task<IActionResult> GetFilmesEmCartaz()
        {
            try
            {
                var nosMovies = await ScrapeCinemaNos();
                var cineplaceMovies = await ScrapeCineplace();
                var cineplaceEnriched = await EnrichWithTmdb(cineplaceMovies);

                var allMovies = nosMovies.Concat(cineplaceEnriched).ToList();

                if (allMovies.Count == 0)
                    return Ok(GetMockCinemaMovies());

                return Ok(allMovies);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraping failed: {ex.Message}");
                return Ok(GetMockCinemaMovies());
            }
        }

        [HttpGet("search-tmdb")]
        public async Task<IActionResult> SearchTmdb([FromQuery] string titulo)
        {
            var apiKey = _configuration["ExternalApis:TmdbApiKey"];
            var anoAtual = DateTime.Now.Year;

            foreach (var ano in new[] { anoAtual, anoAtual - 1, 0 })
            {
                var urlQuery = ano > 0
                    ? $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(titulo)}&language=pt-PT&year={ano}"
                    : $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(titulo)}&language=pt-PT";

                var response = await _httpClient.GetAsync(urlQuery);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var results = data.GetProperty("results");

                if (results.GetArrayLength() == 0) continue;

                var tituloNorm = titulo.ToLower().Trim();
                int? bestId = null;

                for (int i = 0; i < results.GetArrayLength(); i++)
                {
                    var r = results[i];
                    var ptTitle = r.TryGetProperty("title", out var t) ? t.GetString()?.ToLower().Trim() : "";
                    var origTitle = r.TryGetProperty("original_title", out var ot) ? ot.GetString()?.ToLower().Trim() : "";

                    if (ptTitle == tituloNorm || origTitle == tituloNorm)
                    {
                        bestId = r.GetProperty("id").GetInt32();
                        break;
                    }
                }

                if (bestId == null && ano > 0)
                    bestId = results[0].GetProperty("id").GetInt32();

                if (bestId != null)
                    return Ok(new { id = bestId });
            }

            return NotFound();
        }

        private async Task<List<CinemaMovieDto>> EnrichWithTmdb(List<CinemaMovieDto> movies)
        {
            var apiKey = _configuration["ExternalApis:TmdbApiKey"];
            var anoAtual = DateTime.Now.Year;

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

                    JsonElement results = default;

                    foreach (var ano in new[] { anoAtual, anoAtual - 1, 0 })
                    {
                        var url = ano > 0
                            ? $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(tituloLimpo)}&language=pt-PT&year={ano}"
                            : $"https://api.themoviedb.org/3/search/movie?api_key={apiKey}&query={Uri.EscapeDataString(tituloLimpo)}&language=pt-PT";

                        var response = await _httpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode) continue;

                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<JsonElement>(json);
                        results = data.GetProperty("results");
                        if (results.GetArrayLength() > 0) break;
                    }

                    if (results.GetArrayLength() == 0) continue;

                    var tituloNorm = tituloLimpo.ToLower().Trim();
                    JsonElement best = results[0];

                    for (int i = 0; i < results.GetArrayLength(); i++)
                    {
                        var r = results[i];
                        var ptTitle = r.TryGetProperty("title", out var t) ? t.GetString()?.ToLower().Trim() : "";
                        var origTitle = r.TryGetProperty("original_title", out var ot) ? ot.GetString()?.ToLower().Trim() : "";
                        if (ptTitle == tituloNorm || origTitle == tituloNorm) { best = r; break; }
                    }

                    var tmdbId = best.GetProperty("id").GetInt32();

                    var detailsResponse = await _httpClient.GetAsync(
                        $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={apiKey}&language=pt-PT");

                    if (!detailsResponse.IsSuccessStatusCode) continue;

                    var detailsJson = await detailsResponse.Content.ReadAsStringAsync();
                    var details = JsonSerializer.Deserialize<JsonElement>(detailsJson);

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
                    Console.WriteLine($"Erro ao enriquecer filme {movie.Titulo}: {ex.Message}");
                }
            }

            return movies
                    .GroupBy(m => m.Titulo.ToLower().Trim())
                    .Select(g => g.First())
                    .ToList();
        }

        private async Task<List<CinemaMovieDto>> ScrapeCinemaNos()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync("https://www.cinemas.nos.pt/filmes", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });
            await page.WaitForTimeoutAsync(3000);

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
                            Id = $"nos-{i}",
                            Titulo = titulo.Trim(),
                            Poster = imgSrc.StartsWith("//") ? "https:" + imgSrc : imgSrc,
                            Cinema = "Cinema NOS",
                            Horarios = new List<string>(),
                            Genero = genero.Trim(),
                            Duracao = duracao,
                            Classificacao = classificacao,
                            Idioma = "Legendado",
                            Sala = "",
                            Link = link
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no card {i}: {ex.Message}");
                }
            }

            return movies;
        }

        private async Task<List<CinemaMovieDto>> ScrapeCineplace()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync("https://cineplace.pt/filmes/", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });
            await page.WaitForTimeoutAsync(3000);

            var cards = await page.QuerySelectorAllAsync(".movie");
            var movies = new List<CinemaMovieDto>();

            for (int i = 0; i < cards.Count; i++)
            {
                try
                {
                    var card = cards[i];

                    var titulo = await card.EvalOnSelectorAsync<string>("h5", "el => el.innerText") ?? "";
                    var link = await card.EvalOnSelectorAsync<string>(".movie-action-info", "el => el.getAttribute('href')") ?? "";

                    if (!string.IsNullOrEmpty(titulo))
                    {
                        movies.Add(new CinemaMovieDto
                        {
                            Id = $"cineplace-{i}",
                            Titulo = titulo.Trim(),
                            Poster = "",
                            Cinema = "Cineplace",
                            Horarios = new List<string>(),
                            Genero = "",
                            Duracao = "",
                            Classificacao = "N/A",
                            Idioma = "Legendado",
                            Sala = "",
                            Link = link
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no card Cineplace {i}: {ex.Message}");
                }
            }

            return movies;
        }

        private List<CinemaMovieDto> GetMockCinemaMovies()
        {
            return new List<CinemaMovieDto>
            {
                new()
                {
                    Id = "nos-1",
                    Titulo = "Duna: Parte Dois",
                    Poster = "https://image.tmdb.org/t/p/w500/d5NXSklWG3bVgiQ9dYBHWd2Kvbe.jpg",
                    Cinema = "Cinema NOS",
                    Horarios = new List<string> { "14:30", "17:45", "21:00", "23:30" },
                    Genero = "Ficção Científica",
                    Duracao = "2h 46min",
                    Classificacao = "M/12",
                    Idioma = "Legendado",
                    Sala = "Sala 1 - 3D",
                    Link = "https://www.cinemas.nos.pt/filmes/duna-parte-dois"
                },
                new()
                {
                    Id = "nos-2",
                    Titulo = "Oppenheimer",
                    Poster = "https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1sZ2T.jpg",
                    Cinema = "Cinema NOS",
                    Horarios = new List<string> { "15:00", "18:30", "22:00" },
                    Genero = "Drama/História",
                    Duracao = "3h 0min",
                    Classificacao = "M/16",
                    Idioma = "Legendado",
                    Sala = "Sala IMAX",
                    Link = "https://www.cinemas.nos.pt/filmes/oppenheimer"
                },
                new()
                {
                    Id = "nos-3",
                    Titulo = "Guardiões da Galáxia Vol. 3",
                    Poster = "https://image.tmdb.org/t/p/w500/r2J02Z2OpNTctfOSN1Ydgii51I3.jpg",
                    Cinema = "Cinema NOS",
                    Horarios = new List<string> { "13:15", "16:20", "19:30", "22:40" },
                    Genero = "Aventura/Comédia",
                    Duracao = "2h 30min",
                    Classificacao = "M/12",
                    Idioma = "Dublado",
                    Sala = "Sala 4",
                    Link = "https://www.cinemas.nos.pt/filmes/guardioes-da-galaxia-vol-3"
                }
            };
        }

        public class CinemaMovieDto
        {
            public string Id { get; set; } = "";
            public string Titulo { get; set; } = "";
            public string Poster { get; set; } = "";
            public string Cinema { get; set; } = "";
            public List<string> Horarios { get; set; } = new();
            public string Genero { get; set; } = "";
            public string Duracao { get; set; } = "";
            public string Classificacao { get; set; } = "";
            public string Idioma { get; set; } = "";
            public string Sala { get; set; } = "";
            public string Link { get; set; } = "";
        }
    }
}