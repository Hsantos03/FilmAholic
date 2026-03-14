using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Security.Claims;
using System.Text.Json;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CinemaController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly FilmAholicDbContext _context;

        public CinemaController(IConfiguration configuration, IHttpClientFactory httpClientFactory, FilmAholicDbContext context)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _context = context;
        }    

        [HttpGet("em-cartaz")]
        public async Task<IActionResult> GetFilmesEmCartaz()
        {
            try
            {
                var nosMovies = await ScrapeCinemaNos();
                var cinemaCityMovies = await ScrapeCinemaCity();
                var enriched = await EnrichWithTmdb(cinemaCityMovies);
                foreach (var m in enriched) m.Cinema = "Cinema City";

                var allMovies = nosMovies.Concat(enriched).ToList();

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

        /// <summary>Lista de cinemas com nome, morada e coordenadas para o mapa de cinemas próximos.</summary>
        [HttpGet("proximos")]
        public IActionResult GetCinemasProximos()
        {
            var cinemas = new List<CinemaVenueDto>
                {
                // ── CINEMA NOS ───

                new() { Id = "nos-colombo",
                        Nome    = "Cinema NOS - Centro Colombo",
                        Morada  = "Centro Colombo, Av. Lusíada, 1500-392 Lisboa",
                        Latitude = 38.75529551615998, Longitude = -9.187034542329185,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-colombo" },

                new() { Id = "nos-vascogama",
                        Nome    = "Cinema NOS - Vasco da Gama",
                        Morada  = "Centro Comercial Vasco da Gama, Av. D. João II, 1998-014 Lisboa",
                        Latitude = 38.76796111156827, Longitude = -9.09719097116459,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-vasco-da-gama" },

                new() { Id = "nos-amoreiras",
                        Nome    = "Cinema NOS - Amoreiras",
                        Morada  = "Centro Comercial Amoreiras, Av. Eng. Duarte Pacheco, 1070-103 Lisboa",
                        Latitude = 38.72328529305396, Longitude = -9.16164031534163,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-amoreiras" },

                new() { Id = "nos-cascais",
                        Nome    = "Cinema NOS - CascaiShopping",
                        Morada  = "CascaiShopping, Estrada Nacional 7, 2765-543 Alcabideche",
                        Latitude = 38.738333462836565, Longitude = -9.397731342329186,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-cascaishopping" },

                new() { Id = "nos-odivelas",
                        Nome    = "Cinema NOS - Odivelas Strada",
                        Morada  = "Strada Outlet, Estr. da Paiã, 2675-626 Odivelas",
                        Latitude = 38.78241037793091, Longitude = -9.191677428835407,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-odivelas-strada" },

                new() { Id = "nos-alameda",
                        Nome    = "Cinema NOS - Alameda Shop & Spot",
                        Morada  = "R. dos Campeões Europeus 28, 4350-171 Porto",
                        Latitude = 41.1619, Longitude = -8.5847,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alameda-shop-e-spot" },

                new() { Id = "nos-nascente",
                        Nome    = "Cinema NOS - Parque Nascente",
                        Morada  = "Parque Nascente, Praceta Parque Nascente 35, 4435-182 Gondomar",
                        Latitude = 41.17407863150025, Longitude = -8.5660068,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-parque-nascente" },

                new() { Id = "nos-alvalaxia",
                        Nome    = "Cinema NOS - Alvaláxia",
                        Morada  = "Alvaláxia, R. Francisco Stromp, 1600-616 Lisboa",
                        Latitude = 38.7648, Longitude = -9.1578,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alvalaxia" },

                new() { Id = "nos-almashopping",
                        Nome    = "Cinema NOS - Alma Shopping",
                        Morada  = "Alma Shopping, R. Gen. Humberto Delgado 207, 3030-327 Coimbra",
                        Latitude = 40.20475627858824, Longitude = -8.407590871164595,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alma-shopping" },

                new() { Id = "nos-almadaforum",
                        Nome    = "Cinema NOS - Almada Forum",
                        Morada  = "Almada Forum, R. Sérgio Malpique 2, 2810-500 Almada",
                        Latitude = 38.660981075658576, Longitude = -9.175284957670813,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-almada-forum" },

                new() { Id = "nos-braga",
                        Nome    = "Cinema NOS - Braga Parque",
                        Morada  = "Braga Parque, Quinta dos Congregados, 4710-427 Braga",
                        Latitude = 41.55825229097912, Longitude = -8.40524561349378,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-braga-parque" },

                new() { Id = "nos-evora",
                        Nome    = "Cinema NOS - Évora Plaza",
                        Morada  = "Évora Plaza, R. Luís Adelino Fonseca, 7005-345 Évora",
                        Latitude = 38.54980847159133, Longitude = -7.904326384658373,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-evora-plaza" },

                new() { Id = "nos-ferrara",
                        Nome    = "Cinema NOS - Ferrara Plaza",
                        Morada  = "Ferrara Plaza, R. da Carvalhosa, 4590-073 Paços de Ferreira",
                        Latitude = 41.28508754420547, Longitude = -8.36154551534163,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-ferrara-plaza" },

                new() { Id = "nos-forumalgarve",
                        Nome    = "Cinema NOS - Forum Algarve",
                        Morada  = "Forum Algarve, N 125 - KM103, 8005-145 Faro",
                        Latitude = 37.02930818984194, Longitude = -7.946052542329186,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-algarve" },

                new() { Id = "nos-forumcoimbra",
                        Nome    = "Cinema NOS - Forum Coimbra",
                        Morada  = "Forum Coimbra, Av. José Bonifácio de Andrade e Silva 1, 3040-193 Coimbra",
                        Latitude = 40.25114914108218, Longitude = -8.448109816970193,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-coimbra" },

                new() { Id = "nos-forummadeira",
                        Nome    = "Cinema NOS - Forum Madeira",
                        Morada  = "Forum Madeira, Estrada Monumental 390, 9000-250 Funchal",
                        Latitude = 32.63694821367406, Longitude = -16.943635813493778,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-madeira" },

                new() { Id = "nos-forummontijo",
                        Nome    = "Cinema NOS - Forum Montijo",
                        Morada  = "Forum Montijo, R. da Azinheira 1, 2870-100 Montijo",
                        Latitude = 38.69434381494814, Longitude = -8.941345086506223,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-montijo" },

                new() { Id = "nos-fozplaza",
                        Nome    = "Cinema NOS - Foz Plaza",
                        Morada  = "Foz Plaza, R. dos Condados, 3080-216 Buarcos",
                        Latitude = 40.16610166566605, Longitude = -8.859651599999998,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-foz-plaza" },

                new() { Id = "nos-gaiashopping",
                        Nome    = "Cinema NOS - Gaia Shopping",
                        Morada  = "Gaia Shopping, Av. dos Descobrimentos, 4400-241 Vila Nova de Gaia",
                        Latitude = 41.11737475789024, Longitude = -8.622101100000002,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-gaia-shopping" },

                new() { Id = "nos-glicinias",
                        Nome    = "Cinema NOS - Glicínias",
                        Morada  = "Glicínias Plaza, R. Dom Manuel Barbuda e Vasconcelos, 3810-498 Aveiro",
                        Latitude = 40.6263229007036, Longitude = -8.644258044177038,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-glicinias" },

                new() { Id = "nos-maralgarve",
                        Nome    = "Cinema NOS - Mar Algarve Shopping",
                        Morada  = "Mar Algarve Shopping, Av. Algarve Marshopping, 8135-182 Almancil",
                        Latitude = 37.0995559938854, Longitude = -7.9976510423291876,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-mar-algarve-shopping" },

                new() { Id = "nos-marmatosinhos",
                        Nome    = "Cinema NOS - Mar Matosinhos Shopping",
                        Morada  = "Mar Shopping Matosinhos, Av. Mário Moreira Maia, 4450-337 Matosinhos",
                        Latitude = 41.210096904289486, Longitude = -8.686287799999999,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-mar-matosinhos-shopping" },

                new() { Id = "nos-norteshopping",
                        Nome    = "Cinema NOS - NorteShopping",
                        Morada  = "NorteShopping, R. Sara Afonso, 4460-282 Senhora da Hora",
                        Latitude = 41.180729210848625, Longitude = -8.655587786506223,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-norteshopping" },

                new() { Id = "nos-nossoshopping",
                        Nome    = "Cinema NOS - Nosso Shopping",
                        Morada  = "Nosso Shopping, Alameda de Grasse 244, 5000-703 Vila Real",
                        Latitude = 41.29697924174848, Longitude = -7.734671584658371,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-nosso-shopping" },

                new() { Id = "nos-oeirasparque",
                        Nome    = "Cinema NOS - Oeiras Parque",
                        Morada  = "Oeiras Parque, Av. António Bernardo Cabral de Macedo, 2770-219 Paço de Arcos",
                        Latitude = 38.70495773705576, Longitude = -9.302052342329185,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-oeiras-parque" },

                new() { Id = "nos-palaciogelo",
                        Nome    = "Cinema NOS - Palácio do Gelo",
                        Morada  = "Palácio do Gelo Shopping, R. do Palácio do Gelo 200, 3500-606 Viseu",
                        Latitude = 40.64369688599287, Longitude = -7.910722186506222,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-palacio-do-gelo" },

                new() { Id = "nos-parqueatlantico",
                        Nome    = "Cinema NOS - Parque Atlântico",
                        Morada  = "Parque Atlântico, R. da Juventude, 9500-211 Ponta Delgada",
                        Latitude = 38.257732198907846, Longitude = -25.763063212880557,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-parque-atlantico" },

                new() { Id = "nos-torresvedras",
                        Nome    = "Cinema NOS - Torres Vedras",
                        Morada  = "Torres Shopping, R. António Alves Ferreira 2, 2560-256 Torres Vedras",
                        Latitude = 39.102163357018576, Longitude = -9.25283598465837,
                        Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-torres-vedras" },

                // ── CINEMA CITY ──

                new() { Id = "cc-campopequeno",
                        Nome    = "Cinema City - Campo Pequeno",
                        Morada  = "Campo Pequeno, Av. da República, 1000-082 Lisboa",
                        Latitude = 38.74266637076071, Longitude = -9.144644074583022,
                        Website = "https://www.cinemacity.pt/editorial/cinema-city-campo-pequeno/" },

                new() { Id = "cc-leiria",
                        Nome    = "Cinema City - Leiria",
                        Morada  = "Leiria Shopping, R. Virgílio Vieira da Cunha, 2400-447 Leiria",
                        Latitude = 39.74653581023189, Longitude = -8.820864896304302,
                        Website = "https://www.cinemacity.pt/editorial/cinema-city-leiria/" },

                new() { Id = "cc-alfragide",
                        Nome    = "Cinema City - Alegro Alfragide",
                        Morada  = "Alegro Alfragide, Av. dos Cavaleiros 60, 2790-045 Carnaxide",
                        Latitude = 38.72775395220263, Longitude = -9.219047,
                        Website = "https://www.cinemacity.pt/editorial/cinema-city-alegro-alfragide/" },

                new() { Id = "cc-alvalade",
                        Nome    = "Cinema City - Alvalade",
                        Morada  = "Alvalade, Av. de Roma 100, 1700-352 Lisboa",
                        Latitude = 38.75455133960013, Longitude = -9.144403247313472,
                        Website = "https://www.cinemacity.pt/editorial/cinema-city-alvalade/" },

                new() { Id = "cc-algsetubal",
                        Nome    = "Cinema City - Alegro Setúbal",
                        Morada  = "Alegro Setúbal, Av. Antero de Quental, 2910-394 Setúbal",
                        Latitude = 38.53766245049904, Longitude = -8.879256945473498,
                        Website = "https://www.cinemacity.pt/editorial/cinema-city-alegro-setubal/" },
                };
            return Ok(cinemas);
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


        private async Task<List<CinemaMovieDto>> ScrapeCinemaCity()
        {
            var movies = new List<CinemaMovieDto>();
            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://www.cinemacity.pt/", new() { Timeout = 30000 });
                await page.WaitForSelectorAsync(".flip-container", new() { Timeout = 15000 });

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

                        movies.Add(new CinemaMovieDto
                        {
                            Titulo = titulo?.Trim() ?? "",
                            Poster = poster ?? "",
                            Link = link ?? "",
                            Cinema = "Cinema City",
                            Duracao = duracaoText,
                            Classificacao = classificacao,
                            Genero = ""
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fazer scraping do Cinema City: {ex.Message}");
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


        // GET: api/Profile/cinemas-favoritos
        [Authorize]
        [HttpGet("cinemas-favoritos")]
        public async Task<IActionResult> GetCinemasFavoritos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            try
            {
                var favs = string.IsNullOrWhiteSpace(user.CinemasFavoritos)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(user.CinemasFavoritos) ?? new();
                return Ok(favs);
            }
            catch { return Ok(new List<string>()); }
        }

        // POST: api/Profile/cinemas-favoritos/toggle
        [Authorize]
        [HttpPost("cinemas-favoritos/toggle")]
        public async Task<IActionResult> ToggleCinemaFavorito([FromBody] ToggleCinemaFavoritoDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CinemaId))
                return BadRequest();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            List<string> favs;
            try
            {
                favs = string.IsNullOrWhiteSpace(user.CinemasFavoritos)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(user.CinemasFavoritos) ?? new();
            }
            catch { favs = new List<string>(); }

            bool isFav;
            if (favs.Contains(dto.CinemaId))
            {
                favs.Remove(dto.CinemaId);
                isFav = false;
            }
            else
            {
                favs.Add(dto.CinemaId);
                isFav = true;
            }

            user.CinemasFavoritos = JsonSerializer.Serialize(favs);
            await _context.SaveChangesAsync();

            return Ok(new { cinemaId = dto.CinemaId, isFavorito = isFav });
        }


        public class ToggleCinemaFavoritoDto
        {
            public string? CinemaId { get; set; }
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

        public class CinemaVenueDto
        {
            public string Id { get; set; } = "";
            public string Nome { get; set; } = "";
            public string Morada { get; set; } = "";
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string Website { get; set; } = "";
        }
    }
}