using FilmAholic.Server.DTOs;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace FilmAholic.Server.Controllers;

/// <summary>
/// Controlador responsável por expor os endpoints relacionados com os atores/pessoas do cinema.
/// Interage de forma exclusiva com o Serviço de Filmes (<see cref="IMovieService"/>) para recuperar as métricas, detalhes e participações.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AtoresController : ControllerBase
{
    private readonly IMovieService _movieService;

    /// <summary>
    /// Construtor que inicializa o serviço de filmes no controlador de atores.
    /// </summary>
    /// <param name="movieService">A interface de injeção de dependência do tipo de serviço subjacente focado nos filmes e atores.</param>
    public AtoresController(IMovieService movieService)
    {
        _movieService = movieService;
    }

        /// <summary>
        /// Devolve a listagem global dos atores mais populares em destaque atualmente.
        /// </summary>
        /// <param name="count">Número máximo de registos a apresentar por página (Valor por omissão: 100).</param>
        /// <returns>Retorna a resposta HTTP 200 contendo uma lista estruturada de atores em destaque.</returns>
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularActors([FromQuery] int count = 100)
        {
            var actors = await _movieService.GetPopularActorsAsync(page: 1, count: count);
            return Ok(actors);
        }

        /// <summary>
        /// Pesquisa e localiza um ou mais atores pelo seu nome.
        /// </summary>
        /// <param name="query">Nome (ou pedaço do nome) utilizado para fazer a procura do artista.</param>
        /// <returns>Trás de volta a sequência de artistas encontrados compatíveis com o critério. Vazio caso a query seja insuficiente.</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<ActorSearchResultDto>>> SearchActors([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Ok(new List<ActorSearchResultDto>());

            var actors = await _movieService.SearchActorsAsync(query!);
            return Ok(actors);
        }

        /// <summary>
        /// Obtém pormenores exatos relativos à biografia e identificação pessoal num formato de detalhe.
        /// </summary>
        /// <param name="personId">O ID interno ou associado do ator, único nos registos do painel.</param>
        /// <returns>Dados consolidados da entidade individual ou HTTP 404 (Not Found) em caso de não ser descoberto.</returns>
        [HttpGet("{personId:int}")]
        public async Task<ActionResult<ActorDetailsDto>> GetActorDetails([FromRoute] int personId)
        {
            var details = await _movieService.GetActorDetailsAsync(personId);
            if (details == null) return NotFound();
            return Ok(details);
        }

        /// <summary>
        /// Lista de maneira exclusiva todos os filmes em que o ator participa.
        /// </summary>
        /// <param name="personId">O identificador numérico subjacente da entidade na plataforma.</param>
        /// <returns>Traz lista do histórico cronológico ou popular de produções audiovisuais creditadas a esse mesmo perfil.</returns>
        [HttpGet("{personId:int}/movies")]
        public async Task<ActionResult<List<ActorMovieDto>>> GetMoviesByActor([FromRoute] int personId)
        {
            var movies = await _movieService.GetMoviesByActorAsync(personId);
            return Ok(movies);
        }
    }

