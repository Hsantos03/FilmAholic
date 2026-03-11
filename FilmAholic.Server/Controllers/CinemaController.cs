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
                    new() { Id = "nos-colombo",       Nome = "Cinema NOS - Centro Colombo",         Morada = "Centro Colombo, Av. Lusíada, 1500-392 Lisboa",                                        Latitude = 38.7671, Longitude = -9.0935,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-colombo" },
                    new() { Id = "nos-vascogama",     Nome = "Cinema NOS - Centro Vasco da Gama",   Morada = "Centro Comercial Vasco da Gama, Av. D. João II 44, 1990-094 Lisboa",                  Latitude = 38.7622, Longitude = -9.0585,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-vasco-da-gama" },
                    new() { Id = "nos-amoreiras",     Nome = "Cinema NOS - Amoreiras",              Morada = "Centro Comercial das Amoreiras, Av. Eng. Duarte Pacheco, 1070-103 Lisboa",            Latitude = 38.7244, Longitude = -9.1653,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-amoreiras" },
                    new() { Id = "nos-cascais",       Nome = "Cinema NOS - CascaiShopping",         Morada = "CascaiShopping, Av. dos Combatentes da Grande Guerra, 2750-321 Cascais",              Latitude = 38.6974, Longitude = -9.4403,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-cascaishopping" },
                    new() { Id = "nos-odivelas",      Nome = "Cinema NOS - Odivelas Strada",        Morada = "Strada Outlet, R. da Pontinha 1, 2675-411 Odivelas",                                  Latitude = 38.7922, Longitude = -9.1834,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-odivelas-strada" },
                    new() { Id = "nos-alameda",       Nome = "Cinema NOS - Alameda Shop&Spot",      Morada = "R. dos Campeões Europeus de Viena 28-198, 4350-171 Porto",                            Latitude = 41.1619, Longitude = -8.5847,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alameda-shop-e-spot" },
                    new() { Id = "nos-nascente",      Nome = "Cinema NOS - Parque Nascente",        Morada = "Parque Nascente, Rua de Gondomar 691, 4420-520 Gondomar",                             Latitude = 41.1333, Longitude = -8.5333,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-parque-nascente" },
                    new() { Id = "nos-maia",          Nome = "Cinema NOS - Maia Shopping",          Morada = "Maia Shopping, Av. Eng. Duarte Pacheco 383, 4470-154 Maia",                           Latitude = 41.2281, Longitude = -8.6224,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-maia-shopping" },
                    new() { Id = "nos-alvalaxia",     Nome = "Cinema NOS - Alvaláxia",              Morada = "Centro Comercial Alvaláxia, Av. dos Cavaleiros 2, 2675-657 Odivelas",                 Latitude = 38.7834, Longitude = -9.1421,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alvalaxia" },
                    new() { Id = "nos-almashopping",  Nome = "Cinema NOS - Alma Shopping",          Morada = "Alma Shopping, Av. Fernão de Magalhães, 3000-175 Coimbra",                            Latitude = 40.2100, Longitude = -8.4200,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-alma-shopping" },
                    new() { Id = "nos-almadaforum",   Nome = "Cinema NOS - Almada Forum",           Morada = "Almada Forum, Av. D. João II, 2810-001 Almada",                                       Latitude = 38.6762, Longitude = -9.1594,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-almada-forum" },
                    new() { Id = "nos-braga",         Nome = "Cinema NOS - Braga Parque",           Morada = "Braga Parque, Av. da Liberdade, 4710-251 Braga",                                      Latitude = 41.5501, Longitude = -8.4200,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-braga-parque" },
                    new() { Id = "nos-evora",         Nome = "Cinema NOS - Évora Plaza",            Morada = "Évora Plaza, Av. Túlio Espanca, 7000-000 Évora",                                      Latitude = 38.5674, Longitude = -7.9076,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-evora-plaza" },
                    new() { Id = "nos-ferrara",       Nome = "Cinema NOS - Ferrara Plaza",          Morada = "Ferrara Plaza, Av. Cidade de Faro, 2830-001 Barreiro",                                Latitude = 38.6600, Longitude = -9.0700,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-ferrara-plaza" },
                    new() { Id = "nos-forumalgarve",  Nome = "Cinema NOS - Forum Algarve",          Morada = "Forum Algarve, Rua Dr. Cândido Guerreiro, 8000-315 Faro",                             Latitude = 37.0194, Longitude = -7.9322,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-algarve" },
                    new() { Id = "nos-forumcoimbra",  Nome = "Cinema NOS - Forum Coimbra",          Morada = "Forum Coimbra, Rua General Humberto Delgado, 3030-001 Coimbra",                       Latitude = 40.1920, Longitude = -8.4100,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-coimbra" },
                    new() { Id = "nos-forummadeira",  Nome = "Cinema NOS - Forum Madeira",          Morada = "Forum Madeira, Estrada Monumental 390, 9000-100 Funchal",                             Latitude = 32.6400, Longitude = -16.9200, Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-madeira" },
                    new() { Id = "nos-forummontijo",  Nome = "Cinema NOS - Forum Montijo",          Morada = "Forum Montijo, Av. dos Bombeiros Voluntários, 2870-001 Montijo",                      Latitude = 38.7050, Longitude = -8.9750,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-forum-montijo" },
                    new() { Id = "nos-fozplaza",      Nome = "Cinema NOS - Foz Plaza",              Morada = "Foz Plaza, Av. do Brasil 843, 4150-150 Porto",                                        Latitude = 41.1560, Longitude = -8.6760,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-foz-plaza" },
                    new() { Id = "nos-gaiashopping",  Nome = "Cinema NOS - Gaia Shopping",          Morada = "Gaia Shopping, Av. dos Descobrimentos, 4400-107 Vila Nova de Gaia",                   Latitude = 41.1200, Longitude = -8.6200,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-gaia-shopping" },
                    new() { Id = "nos-glicinias",     Nome = "Cinema NOS - Glicínias",              Morada = "Glicínias Plaza, Av. Dr. Lourenço Peixinho, 3800-159 Aveiro",                         Latitude = 40.6440, Longitude = -8.6450,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-glicinias" },
                    new() { Id = "nos-maralgarve",    Nome = "Cinema NOS - Mar Algarve Shopping",   Morada = "Mar Algarve Shopping, EN 125, 8500-802 Portimão",                                     Latitude = 37.1380, Longitude = -8.5380,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-mar-algarve-shopping" },
                    new() { Id = "nos-marmatosinhos", Nome = "Cinema NOS - Mar Matosinhos Shopping",Morada = "Mar Shopping Matosinhos, Rua de Santana, 4450-227 Matosinhos",                        Latitude = 41.1850, Longitude = -8.6900,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-mar-matosinhos-shopping" },
                    new() { Id = "nos-norteshopping", Nome = "Cinema NOS - NorteShopping",          Morada = "NorteShopping, R. Sara Afonso, 4460-841 Senhora da Hora",                             Latitude = 41.1950, Longitude = -8.6550,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-norteshopping" },
                    new() { Id = "nos-nossoshopping", Nome = "Cinema NOS - Nosso Shopping",         Morada = "Nosso Shopping, Av. Infante D. Henrique, 2685-338 Póvoa de Santa Iria",               Latitude = 38.8600, Longitude = -9.0700,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-nosso-shopping" },
                    new() { Id = "nos-oeirasparque",  Nome = "Cinema NOS - Oeiras Parque",          Morada = "Oeiras Parque, Estrada Nacional 249-4, 2780-159 Oeiras",                              Latitude = 38.6970, Longitude = -9.3070,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-oeiras-parque" },
                    new() { Id = "nos-palaciogelo",   Nome = "Cinema NOS - Palácio do Gelo",        Morada = "Palácio do Gelo Shopping, Rua Palácio do Gelo, 3500-606 Viseu",                       Latitude = 40.6600, Longitude = -7.9100,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-palacio-do-gelo" },
                    new() { Id = "nos-parqueatlantico",Nome = "Cinema NOS - Parque Atlântico",      Morada = "Parque Atlântico, Rua Eng. José Cordeiro, 9500-801 Ponta Delgada",                    Latitude = 37.7490, Longitude = -25.6690, Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-parque-atlantico" },
                    new() { Id = "nos-portimao",      Nome = "Cinema NOS - Portimão",               Morada = "Aqua Portimão, Rua de São Pedro, 8500-551 Portimão",                                  Latitude = 37.1360, Longitude = -8.5380,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-portimao" },
                    new() { Id = "nos-torresvedras",  Nome = "Cinema NOS - Torres Vedras",          Morada = "Torres Shopping, Av. dos Bombeiros Municipais, 2560-000 Torres Vedras",               Latitude = 39.0900, Longitude = -9.2600,  Website = "https://www.cinemas.nos.pt/cinemas?theater=cinemas-nos-torres-vedras" },
                    new() { Id = "cc-campopequeno",   Nome = "Cinema City - Campo Pequeno",         Morada = "Campo Pequeno, Av. da República, 1000-056 Lisboa",                                    Latitude = 38.7369, Longitude = -9.1488,  Website = "https://www.cinemacity.pt/editorial/cinema-city-campo-pequeno/" },
                    new() { Id = "cc-leiria",         Nome = "Cinema City - Leiria",                Morada = "Leiria Shopping, Rua Mestre de Avis, 2400-239 Leiria",                                Latitude = 39.7436, Longitude = -8.8071,  Website = "https://www.cinemacity.pt/editorial/cinema-city-leiria/" },
                    new() { Id = "cc-alfragide",      Nome = "Cinema City - Alegro Alfragide",      Morada = "Alegro Alfragide, Rua São João de Deus, 2610-172 Alfragide",                          Latitude = 38.7300, Longitude = -9.2100,  Website = "https://www.cinemacity.pt/editorial/cinema-city-alegro-alfragide/" },
                    new() { Id = "cc-alvalade",       Nome = "Cinema City - Alvalade",              Morada = "Praça de Alvalade 10, 1700-036 Lisboa",                                               Latitude = 38.7530, Longitude = -9.1420,  Website = "https://www.cinemacity.pt/editorial/cinema-city-alvalade/" },
                    new() { Id = "cc-algsetubal",     Nome = "Cinema City - Alegro Setúbal",        Morada = "Alegro Setúbal, Av. Luísa Todi 300, 2900-452 Setúbal",                                Latitude = 38.5244, Longitude = -8.8882,  Website = "https://www.cinemacity.pt/editorial/cinema-city-alegro-setubal/" },
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