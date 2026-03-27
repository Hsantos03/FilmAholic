using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class CinemaProximosDataIntegrityTests : IDisposable
    {
        private readonly CinemaController _controller;
        private readonly FilmAholicDbContext _context;

        public CinemaProximosDataIntegrityTests()
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
        public void Cinemas_Coordinates_AreValid()
        {
            foreach (var c in GetCinemas())
            {
                Assert.InRange(c.Latitude, -90, 90);
                Assert.InRange(c.Longitude, -180, 180);
            }
        }

        [Fact]
        public void Cinemas_HaveNameAndAddress()
        {
            foreach (var c in GetCinemas())
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Nome));
                Assert.False(string.IsNullOrWhiteSpace(c.Morada));
            }
        }

        [Fact]
        public void Cinemas_Names_AreUnique()
        {
            var cinemas = GetCinemas();
            var duplicates = cinemas.GroupBy(c => c.Nome).Where(g => g.Count() > 1);

            Assert.Empty(duplicates);
        }

        [Fact]
        public void Cinemas_Coordinates_NotZero()
        {
            foreach (var c in GetCinemas())
            {
                Assert.NotEqual(0, c.Latitude);
                Assert.NotEqual(0, c.Longitude);
            }
        }

        [Fact]
        public void Cinemas_Websites_Valid()
        {
            foreach (var c in GetCinemas())
            {
                if (!string.IsNullOrEmpty(c.Website))
                    Assert.StartsWith("http", c.Website);
            }
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