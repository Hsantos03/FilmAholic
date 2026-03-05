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
                var movies = await ScrapeCinemaNos();
                if (movies == null || movies.Count == 0)
                    movies = GetMockCinemaMovies();
                return Ok(movies);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Scraping failed, using mock data: {ex.Message}");
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