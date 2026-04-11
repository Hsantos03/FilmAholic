using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.Json;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Ponto genérico de controlo da listagem, busca e curadoria dos filmes registados ou remotos do projeto API.
    /// Providencia a agregação para detalhes profundos, integração com TMDB e trailers interativos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FilmesController : ControllerBase
    {
        private readonly IMovieService _movieService;
        private readonly FilmAholicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Construtor matriz base que constrói este terminal a partir de HTTP Factories e gestores de injeção.
        /// </summary>
        /// <param name="movieService">Os recursos utilitários isolados para manipulação de filmes.</param>
        /// <param name="context">Entity framework Db Context subjacente e responsável pelas sessões diretas do repositório.</param>
        /// <param name="configuration">Variáveis de leitura no appSettings face às chaves de acesso externas (Ex: YouTube).</param>
        /// <param name="httpClientFactory">Disposição para aceder a clientes http não vinculados (usado para APIs diretas não integradas nativamente).</param>
        public FilmesController(IMovieService movieService, FilmAholicDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _movieService = movieService;
            _context = context;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Obter todos os filmes arquivados localmente. Tem a precaução proactiva de capturar os populares caso o banco local conste vazio (Ex: 1ª utilização).
        /// </summary>
        /// <returns>Tabela contendo os registos serializados.</returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var dbMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            if (dbMovies.Count < 10)
            {
                try
                {
                    var popularMovies = await _movieService.GetPopularMoviesAsync(page: 1, count: 20);
                    if (popularMovies.Any())
                    {
                        foreach (var movie in popularMovies)
                        {
                            var existing = await _context.Set<Models.Filme>()
                                .FirstOrDefaultAsync(f => 
                                    (!string.IsNullOrEmpty(movie.TmdbId) && f.TmdbId == movie.TmdbId) ||
                                    f.Titulo == movie.Titulo);
                            
                            if (existing == null)
                            {
                                movie.Id = 0;
                                _context.Set<Models.Filme>().Add(movie);
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception)
                {
                    if (!dbMovies.Any())
                    {
                        return Ok(FilmSeed.Filmes);
                    }
                }
            }

            var allMovies = await _context.Set<Models.Filme>().ToListAsync();
            
            if (!allMovies.Any())
            {
                return Ok(FilmSeed.Filmes);
            }

            return Ok(allMovies);
        }

        /// <summary>
        /// Inicia pesquisa exata via motor próprio de busca. Funciona suportando a paginação e a query via TMDB API.
        /// </summary>
        /// <param name="query">A designação literal total ou por blocos da longa metragem ansiada.</param>
        /// <param name="page">A secção da página a puxar (padrão é de 1).</param>
        /// <returns>A lista JSON das ocorrências equiparadas ou aproximação da frase da query.</returns>
        [HttpGet("search")]
        public async Task<IActionResult> SearchMovies([FromQuery] string query, [FromQuery] int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required.");
            }

            try
            {
                var result = await _movieService.SearchMoviesAsync(query, page);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while searching movies.", details = ex.Message });
            }
        }

        /// <summary>
        /// TMDB: lista de próximos lançamentos (paginação do TMDB acumulada).
        /// Revalida a data atual em UTC para devolver estritamente antecipados não transacionados em sala.
        /// </summary>
        /// <param name="page">Ponto de partida logarítmico (paginação).</param>
        /// <param name="count">O teto a alcançar, forçado num máximo de cerca de 40 para garantir integridade do tráfego.</param>
        /// <returns>Produções confirmadas ainda sob alçada global da não-estreia.</returns>
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcoming([FromQuery] int page = 1, [FromQuery] int count = 20)
        {
            if (page < 1) page = 1;
            if (count < 1) count = 20;
            count = Math.Min(count, 40);

            try
            {
                var todayUtc = DateTime.UtcNow.Date;

                // Uma página TMDB tem ~20 títulos; muitos podem já ter estreado — percorremos páginas até encher `count`.
                var list = await _movieService.GetUpcomingMoviesAccumulatedAsync(page, count, todayUtc, maxPagesToScan: 12);

                return Ok(list ?? new List<Models.Filme>());
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter upcoming do TMDB.", details = ex.Message });
            }
        }


        /// <summary>
        /// Filmes “Clássicos” via TMDB: nota + construtos minímos em avaliadores (geralmente fixos a mais de 500 votos para legitimar).
        /// Aceita diferentes canais e fontes (Discovery normal contra o Top_Rated TMDB).
        /// </summary>
        /// <param name="fonte">Raminho de pesquisa base (discover / top_rated).</param>
        /// <param name="page">Folha de resultados numéricos.</param>
        /// <param name="count">Número massificado de quantos filmes pretendidos.</param>
        /// <param name="ateData">Filtro teto contendo a data máxima para balizar "Clássico".</param>
        /// <param name="minVotos">Filtragem arbitrária impedindo filmes obscuros com pontuações altíssimamente enviesadas.</param>
        /// <returns>Matriz com os ícones mais reputados sob os filtros exigidos.</returns>
        [HttpGet("classicos")]
        public async Task<IActionResult> GetClassicos(
            [FromQuery] string fonte = "discover",
            [FromQuery] int page = 1,
            [FromQuery] int count = 20,
            [FromQuery] string? ateData = null,
            [FromQuery] int minVotos = 500)
        {
            if (page < 1) page = 1;
            if (count < 1) count = 20;
            count = Math.Min(count, 40);

            try
            {
                var key = fonte.Trim().ToLowerInvariant().Replace("-", "_");
                List<Models.Filme> list = key switch
                {
                    "top_rated" or "toprated" => await _movieService.GetTopRatedMoviesAsync(page, count),
                    _ => await _movieService.GetClassicDiscoverMoviesAsync(page, count, ateData, minVotos)
                };

                return Ok(list);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter filmes clássicos.", details = ex.Message });
            }
        }

        /// <summary>
        /// Fornece o leque de todos os campos específicos de um filme, através de um sistema decrescente de queda para procura (Base de dados interna Local ID -> TMDB ID Local -> TMDB API call de contingência).
        /// </summary>
        /// <param name="id">A chave remota única ou código primário indexado.</param>
        /// <returns>As propriedades expandidas de forma exclusiva à peça audiovisual.</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            // Procura na BD local pelo ID
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            // Procura na BD pelo TmdbId
            if (filme == null)
                filme = await _context.Set<Models.Filme>()
                    .FirstOrDefaultAsync(f => f.TmdbId == id.ToString());

            // Procura no seed
            if (filme == null)
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);

            // Se ainda n�o encontrou, vai buscar ao TMDB
            if (filme == null)
            {
                try
                {
                    filme = await _movieService.GetOrCreateMovieFromTmdbAsync(id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao buscar filme do TMDB: {ex.Message}");
                    return NotFound();
                }
            }

            if (filme == null) return NotFound();

            return Ok(filme);
        }

        /// <summary>
        /// Encaminha o pedido de recolha das meta tags numéricas exatas apenas pela vertente oficial remota TMDb sem compromissos com local.
        /// </summary>
        /// <param name="tmdbId">ID gerado pela TheMovieDataBase Oficial.</param>
        /// <returns>Representação íntegra descarregada online do perfil cinemático.</returns>
        [HttpGet("tmdb/{tmdbId}")]
        public async Task<IActionResult> GetMovieFromTmdb(int tmdbId)
        {
            try
            {
                var movie = await _movieService.GetMovieInfoAsync(tmdbId);
                if (movie == null)
                {
                    return NotFound(new { error = $"Movie with TMDb ID {tmdbId} not found." });
                }

                return Ok(movie);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while fetching movie details.", details = ex.Message });
            }
        }

        /// <summary>
        /// Transpõe intencionalmente a peça em bruto via repositório TMDb integrando e forçando a estada e cacheamento interno deste para o banco local.
        /// </summary>
        /// <param name="tmdbId">O identificativo que remanesce na ligação remota e unicamente lá.</param>
        /// <returns>Os dados processados, convertidos e criados no lado da BD, com o novo índice interno.</returns>
        [HttpPost("tmdb/{tmdbId}")]
        public async Task<IActionResult> AddMovieFromTmdb(int tmdbId)
        {
            try
            {
                var movie = await _movieService.GetOrCreateMovieFromTmdbAsync(tmdbId);
                return Ok(movie);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while adding movie.", details = ex.Message });
            }
        }

        
        /// <summary>
        /// Força a renovação parcial de conteúdo de um filme interno, recorrendo novamente as comunicações com as APIs para limpar discrepâncias ou lacunas face à data atual.
        /// </summary>
        /// <param name="id">Local ID mapeado para encontrar correspondência com o TMDB Id atachado.</param>
        /// <returns>Atualiza e regressa a versão mutada pelo update.</returns>
        [HttpPut("{id}/update")]
        public async Task<IActionResult> UpdateMovie(int id)
        {
            try
            {
                var movie = await _movieService.UpdateMovieFromApisAsync(id);
                if (movie == null)
                {
                    return NotFound(new { error = $"Movie with ID {id} not found." });
                }

                return Ok(movie);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating movie.", details = ex.Message });
            }
        }

        /// <summary>
        /// Agrega as votações globais originárias do TMDb associadas com a película procurada.
        /// </summary>
        /// <param name="id">O referencial que pode ser interno ou em fase de fallback o número serial do TMDb em si.</param>
        /// <returns>Uma entidade de listagem das votações críticas.</returns>
        [HttpGet("{id}/ratings")]
        public async Task<IActionResult> GetRatings(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            if (filme == null)
            {
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            var ratings = await _movieService.GetRatingsAsync(filme.TmdbId, filme.Titulo);

            return Ok(ratings);
        }

        /// <summary>
        /// Baseado intimamente com os pares recomendados para a obra especificada (O "Filmes Similares a este...").
        /// Transpõe resultados nativos de IA baseados pelas semelhanças das fichas técnicas via TMDB.
        /// </summary>
        /// <param name="id">Chave de reconhecimento da peça (Interno ou TMDB direto).</param>
        /// <param name="count">O somatório das sugestões adjuntas retornáveis.</param>
        /// <returns>Uma panóplia em loop de objectos <see cref="Models.Filme"/> semanticamente iguais.</returns>
        [HttpGet("{id}/recomendacoes")]
        public async Task<IActionResult> GetRecommendations(int id, [FromQuery] int count = 10)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);

            if (filme == null)
            {
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);
                if (filme == null) return NotFound();
            }

            if (string.IsNullOrEmpty(filme.TmdbId) || !int.TryParse(filme.TmdbId, out var tmdbId))
            {
                try
                {
                    var searchResult = await _movieService.SearchMoviesAsync(filme.Titulo, 1);
                    var match = searchResult?.Results?.FirstOrDefault();
                    if (match != null)
                    {
                        tmdbId = match.Id;
                    }
                    else
                    {
                        return Ok(new List<Models.Filme>()); 
                    }
                }
                catch
                {
                    return Ok(new List<Models.Filme>()); 
                }
            }

            if (count <= 0) count = 10;
            if (count > 20) count = 20;

            var recommendations = await _movieService.GetRecommendationsAsync(tmdbId, count);
            return Ok(recommendations);
        }

        /// <summary>
        /// Obtêm todo o repertório humano associado e creditado à obra de referência.
        /// Inclui diretores, atrizes secundárias, realizadores.
        /// </summary>
        /// <param name="id">Índex da produção.</param>
        /// <returns>Vetor denso listando e providenciando fotografias, papéis interpretativos e nomes legais das equipas envolvidas.</returns>
        [HttpGet("{id}/cast")]
        public async Task<IActionResult> GetCast(int id)
        {
            var filme = await _context.Set<Models.Filme>().FindAsync(id);
            if (filme == null)
                filme = FilmSeed.Filmes.FirstOrDefault(f => f.Id == id);

            int tmdbId;
            if (filme != null && !string.IsNullOrEmpty(filme.TmdbId) && int.TryParse(filme.TmdbId, out var parsed))
            {
                tmdbId = parsed;
            }
            else
            {
                tmdbId = id;
            }

            var cast = await _movieService.GetCastAsync(tmdbId);
            return Ok(cast);
        }

        /// <summary>
        /// Apanha a antevisão promocional visual oficial veiculada via Plataformas Digitais de Video (Youtube).
        /// Estabelece uma conexão Http bruta ao feed de Videos do TMDB perante o ID e formatação linguistica ("pt-PT" vs "en-US") regressando preferencialmente um Trailer.
        /// </summary>
        /// <param name="id">Referência à obra pretendida.</param>
        /// <returns>Devolve uma URL crua pronta a ser embebida ou percurrida se disponível de facto, 404 caso não desponte.</returns>
        [HttpGet("{id:int}/trailer")]
        public async Task<IActionResult> GetTrailer(int id)
        {
            var apiKey = _configuration["ExternalApis:TmdbApiKey"];
            var _httpClient = _httpClientFactory.CreateClient();

            string? trailerKey = null;

            foreach (var lang in new[] { "pt-PT", "en-US" })
            {
                var response = await _httpClient.GetAsync(
                    $"https://api.themoviedb.org/3/movie/{id}/videos?api_key={apiKey}&language={lang}");

                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);
                var results = data.GetProperty("results");

                foreach (var v in results.EnumerateArray())
                {
                    var type = v.TryGetProperty("type", out var t) ? t.GetString() : "";
                    var site = v.TryGetProperty("site", out var s) ? s.GetString() : "";
                    if (type == "Trailer" && site == "YouTube")
                    {
                        trailerKey = v.GetProperty("key").GetString();
                        break;
                    }
                }

                if (trailerKey != null) break;
            }

            if (trailerKey == null) return NotFound();

            return Ok(new { url = $"https://www.youtube.com/watch?v={trailerKey}" });
        }

        /// <summary>
        /// Filmes mais populares do TMDB com mínimo de 500 classificações enraigado.
        /// Busca da API remota as entidades populares e reduz com firmeza aos que têm uma densidade notável na comunidade.
        /// </summary>
        /// <param name="count">Máximo listado pretendido na chamada.</param>
        /// <param name="minRatings">O filtro limitador para que possua o selo social.</param>
        /// <returns>Enchimento paginado massivo.</returns>
        [HttpGet("populares-comunidade")]
        public async Task<IActionResult> GetPopularCommunityMovies([FromQuery] int count = 10, [FromQuery] int minRatings = 500)
        {
            if (count < 1) count = 10;
            if (count > 40) count = 40;
            if (minRatings < 1) minRatings = 500;

            try
            {
                // Buscar filmes populares do TMDB com mínimo de votos
                var movies = await _movieService.GetPopularMoviesWithMinVotesAsync(count, minRatings, maxPages: 10);
                return Ok(movies);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Erro ao obter filmes populares do TMDB.", details = ex.Message });
            }
        }
    }
}
