using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FilmAholic.Tests.UnitTests
{
    public class CinemaProximosUnitTests : IDisposable
    {
        private readonly CinemaController _controller;
        private readonly FilmAholicDbContext _context;

        public CinemaProximosUnitTests()
        {
            var options = new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new FilmAholicDbContext(options);

            var configMock = new Mock<IConfiguration>();
            var httpMock = new Mock<IHttpClientFactory>();
            httpMock.Setup(x => x.CreateClient(It.IsAny<string>()))
                    .Returns(new HttpClient());

            _controller = new CinemaController(configMock.Object, httpMock.Object, _context);
        }

        [Fact]
        public void GetCinemasProximos_ReturnsOk()
        {
            var result = _controller.GetCinemasProximos();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public void GetCinemasProximos_NotEmpty()
        {
            var cinemas = GetCinemas();
            Assert.NotEmpty(cinemas);
        }

        [Fact]
        public void GetCinemasProximos_IdsUnique()
        {
            var cinemas = GetCinemas();
            Assert.Equal(cinemas.Count, cinemas.Select(c => c.Id).Distinct().Count());
        }

        [Fact]
        public void GetCinemasProximos_AllHaveCoordinates()
        {
            foreach (var c in GetCinemas())
            {
                Assert.NotEqual(default, c.Latitude);
                Assert.NotEqual(default, c.Longitude);
            }
        }

        [Fact]
        public void GetCinemasProximos_Performance()
        {
            var start = DateTime.Now;

            _controller.GetCinemasProximos();

            var duration = DateTime.Now - start;

            Assert.True(duration.TotalMilliseconds < 100);
        }

        private List<CinemaController.CinemaVenueDto> GetCinemas()
        {
            var result = _controller.GetCinemasProximos();
            var ok = (OkObjectResult)result;
            return ((IEnumerable<CinemaController.CinemaVenueDto>)ok.Value).ToList();
        }

        public void Dispose() => _context.Dispose();
    }
}