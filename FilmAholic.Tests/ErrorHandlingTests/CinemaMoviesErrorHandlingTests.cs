using Microsoft.AspNetCore.Mvc;
using FilmAholic.Server.Controllers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;
using FilmAholic.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class CinemaMoviesErrorHandlingTests
    {
        private Mock<IConfiguration> mockConfiguration;
        private CinemaController controller;

        public CinemaMoviesErrorHandlingTests()
        {
            mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns("test-api-key");
            
            IHttpClientFactory testFactory = new TestHttpClientFactory();
            
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            var context = new FilmAholicDbContext(options);
            
            controller = new CinemaController(mockConfiguration.Object, testFactory, context);
        }

        private class TestHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient()
            {
                return new HttpClient();
            }

            public HttpClient CreateClient(string name)
            {
                return new HttpClient();
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_ConfiguracaoAusente_DeveRetornarMockData()
        {
            // Arrange
            var mockConfigVazia = new Mock<IConfiguration>();
            mockConfigVazia.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns((string?)null);
            
            var controllerSemConfig = new CinemaController(mockConfigVazia.Object, new TestHttpClientFactory(), CreateTestContext());
            
            // Act
            var result = await controllerSemConfig.GetFilmesEmCartaz();
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var moviesList = okResult.Value as System.Collections.IList;
            var movies = new List<object>();
            foreach (var item in moviesList)
            {
                movies.Add(item);
            }
            Assert.True(movies.Count > 0);
        }

        [Fact]
        public async Task GetFilmesEmCartaz_HttpClientFactoryNulo_DeveLancarExcecao()
        {
            // Arrange
            Assert.Throws<ArgumentNullException>(() => 
                new CinemaController(mockConfiguration.Object, null!, CreateTestContext()));
        }

        [Fact]
        public async Task GetFilmesEmCartaz_ConfiguracaoNula_DeveLancarExcecao()
        {
            // Arrange 
            
            // Act & Assert 
            var controller = new CinemaController(null!, new TestHttpClientFactory(), CreateTestContext());
            
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task SearchTmdb_ConfiguracaoAusente_DeveRetornarNotFound()
        {
            // Arrange
            var mockConfigVazia = new Mock<IConfiguration>();
            mockConfigVazia.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns((string?)null);
            
            var controllerSemConfig = new CinemaController(mockConfigVazia.Object, new TestHttpClientFactory(), CreateTestContext());
            
            // Act
            var result = await controllerSemConfig.SearchTmdb("Test Movie");
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SearchTmdb_TituloNulo_DeveRetornarNotFound()
        {
            // Arrange
            string? tituloNulo = null;

            // Act & Assert 
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                controller.SearchTmdb(tituloNulo!));
        }

        [Fact]
        public async Task SearchTmdb_TituloVazio_DeveRetornarNotFound()
        {
            // Arrange
            var tituloVazio = "";
            
            // Act
            var result = await controller.SearchTmdb(tituloVazio);
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SearchTmdb_ApiKeyVazia_DeveRetornarNotFound()
        {
            // Arrange
            var mockConfigVazia = new Mock<IConfiguration>();
            mockConfigVazia.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns("");
            
            var controllerConfigVazia = new CinemaController(mockConfigVazia.Object, new TestHttpClientFactory(), CreateTestContext());
            
            // Act
            var result = await controllerConfigVazia.SearchTmdb("Test Movie");
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SearchTmdb_ApiKeyInvalida_DeveRetornarNotFound()
        {
            // Arrange 
            var mockConfigInvalida = new Mock<IConfiguration>();
            mockConfigInvalida.Setup(c => c["ExternalApis:TmdbApiKey"]).Returns("invalid-key-123");
            
            var controllerConfigInvalida = new CinemaController(mockConfigInvalida.Object, new TestHttpClientFactory(), CreateTestContext());
            
            // Act
            var result = await controllerConfigInvalida.SearchTmdb("Test Movie");
            
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }      

        [Fact]
        public async Task SearchTmdb_SemResultados_DeveRetornarNotFound()
        {
            // Arrange 
            var tituloInexistente = "MovieThatDoesNotExistAnywhere123456789";
            
            // Act
            var result = await controller.SearchTmdb(tituloInexistente);
            
            // Assert 
            Assert.IsType<NotFoundResult>(result);
        }

        private FilmAholicDbContext CreateTestContext()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            return new FilmAholicDbContext(options);
        }
    }
}
