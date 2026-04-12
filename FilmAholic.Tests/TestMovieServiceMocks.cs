using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Moq;

namespace FilmAholic.Tests;

/// <summary>
/// Mocks reutilizáveis para controladores que dependem de <see cref="IMovieService"/>.
/// </summary>
public static class TestMovieServiceMocks
{
    /// <summary>
    /// Devolve um filme com Id estável por TMDb para testes que não persistem o resultado real do serviço.
    /// </summary>
    public static IMovieService ForComunidadesController()
    {
        var m = new Mock<IMovieService>();
        m.Setup(x => x.GetOrCreateMovieFromTmdbAsync(It.IsAny<int>()))
            .ReturnsAsync((int tmdbId) => new Filme
            {
                Id = 100000 + tmdbId,
                TmdbId = tmdbId.ToString(),
                Titulo = "Mocked film"
            });
        return m.Object;
    }
}
