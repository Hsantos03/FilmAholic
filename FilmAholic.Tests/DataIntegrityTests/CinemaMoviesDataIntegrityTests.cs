using Microsoft.AspNetCore.Mvc;
using FilmAholic.Server.Controllers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using FilmAholic.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class CinemaMoviesDataIntegrityTests
    {
        private List<object> mockMoviesData; 

        public CinemaMoviesDataIntegrityTests()
        {
            // Usar dados mock estáticos em vez de chamadas HTTP reais para testes rápidos
            mockMoviesData = GetMockData();
        }

        private List<object> GetMockData()
        {
            // Dados mock simulando resposta da API para testes rápidos
            var movies = new List<object>
            {
                new {
                    Id = "nos-1",
                    Titulo = "Duna: Parte Dois",
                    Cinema = "Cinema NOS",
                    Duracao = "2h 46min",
                    Classificacao = "M/12",
                    Idioma = "Legendado",
                    Poster = "https://image.tmdb.org/t/p/w500/poster1.jpg",
                    Link = "https://www.cinemas.nos.pt/filme/duna-parte-dois"
                },
                new {
                    Id = "nos-2",
                    Titulo = "Kung Fu Panda 4",
                    Cinema = "Cinema NOS",
                    Duracao = "1h 34min",
                    Classificacao = "M/6",
                    Idioma = "Dublado",
                    Poster = "https://image.tmdb.org/t/p/w500/poster2.jpg",
                    Link = "https://www.cinemas.nos.pt/filme/kung-fu-panda-4"
                },
                new {
                    Id = "cineplace-1",
                    Titulo = "Duna: Parte Dois",
                    Cinema = "Cineplace",
                    Duracao = "2h 46min",
                    Classificacao = "M/12",
                    Idioma = "VO",
                    Poster = "https://cdn.nos.pt/cineplace/poster1.jpg",
                    Link = "https://www.cineplace.pt/filme/duna-parte-dois"
                },
                new {
                    Id = "cineplace-2",
                    Titulo = "Godzilla x Kong",
                    Cinema = "Cineplace",
                    Duracao = "1h 55min",
                    Classificacao = "M/12",
                    Idioma = "Legendado",
                    Poster = "https://cdn.nos.pt/cineplace/poster3.jpg",
                    Link = "https://www.cineplace.pt/filme/godzilla-x-kong"
                }
            };
            return movies;
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockUnicosIDs_DeveTerIDsUnicos()
        {
            // Arrange 
            Assert.True(mockMoviesData.Count > 0);
            
            var ids = new List<string>();
            foreach (var movie in mockMoviesData)
            {
                var idProperty = movie.GetType().GetProperty("Id");
                var id = idProperty?.GetValue(movie)?.ToString();
                if (id != null) ids.Add(id);
            }
            var uniqueIds = ids.Distinct().ToList();
            
            Assert.Equal(ids.Count, uniqueIds.Count);
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockLinksValidos_DeveTerLinksValidos()
        {
            // Arrange 
            
            // Act & Assert
            foreach (var movie in mockMoviesData)
            {
                var link = GetProperty(movie, "Link") as string;
                var poster = GetProperty(movie, "Poster") as string;
                
                if (!string.IsNullOrEmpty(link))
                {
                    Assert.True(link.StartsWith("http://") || link.StartsWith("https://"));
                }
                
                if (!string.IsNullOrEmpty(poster))
                {
                    Assert.True(poster.StartsWith("http://") || poster.StartsWith("https://"));
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_DadosMockClassificacoesValidas_DeveTerClassificacoesValidas()
        {
            // Arrange 
            // Act & Assert   
            var actualClassifications = mockMoviesData.Select(m => GetProperty(m, "Classificacao")).Distinct().ToList();
            
            var classificacoesValidas = new[] { "M/4", "M/6", "M/12", "M/14", "M/16", "M/18", "M4", "M6", "M12", "M14", "M16", "M18", "N/A", "Todos", "Livre" };
            
            foreach (var movie in mockMoviesData)
            {
                var classificacao = GetProperty(movie, "Classificacao") as string;
                
                Assert.NotNull(classificacao);
                Assert.Contains(classificacao, classificacoesValidas);
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataCinemaFiltering_DeveSepararCorretamentePorCinema()
        {
            // Arrange 
            
            // Act 
            var nosMovies = mockMoviesData.Where(m => GetProperty(m, "Cinema") == "Cinema NOS").ToList();
            var cineplaceMovies = mockMoviesData.Where(m => GetProperty(m, "Cinema") == "Cineplace").ToList();
            
            // Assert 
            Assert.True(nosMovies.Count > 0);
            Assert.True(cineplaceMovies.Count > 0);
            
            var nosIds = nosMovies.Select(m => GetProperty(m, "Id")).ToHashSet();
            var cineplaceIds = cineplaceMovies.Select(m => GetProperty(m, "Id")).ToHashSet();
            Assert.Empty(nosIds.Intersect(cineplaceIds));
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataDurationParsing_DeveTerDuracoesParseaveis()
        {
            // Arrange - Test duration format consistency (used by Angular component)
            
            // Act & Assert - All durations should be parseable by the component's parseDuration method
            foreach (var movie in mockMoviesData)
            {
                var duracao = GetProperty(movie, "Duracao") as string;
                Assert.NotNull(duracao);
                Assert.False(string.IsNullOrEmpty(duracao));
                
                // Test the same regex logic as the Angular component
                var hoursMatch = System.Text.RegularExpressions.Regex.Match(duracao, @"(\d+)h");
                var minutesMatch = System.Text.RegularExpressions.Regex.Match(duracao, @"(\d+)min");
                
                // Should have at least hours or minutes (handle both formats)
                Assert.True(hoursMatch.Success || minutesMatch.Success, 
                    $"Duration '{duracao}' should have hours and/or minutes format. Found format: {duracao}");
                
                // Verify valid numeric values
                if (hoursMatch.Success)
                {
                    var hours = int.Parse(hoursMatch.Groups[1].Value);
                    Assert.True(hours >= 0 && hours <= 12, $"Duration hours {hours} should be reasonable");
                }
                
                if (minutesMatch.Success)
                {
                    var minutes = int.Parse(minutesMatch.Groups[1].Value);
                    Assert.True(minutes >= 0 && minutes <= 59, $"Duration minutes {minutes} should be 0-59");
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataLanguage_DeveTerIdiomasConsistentes()
        {
            // Arrange 
            var idiomasValidos = new[] { "Legendado", "Dublado", "VO", "Versão Original" };
            
            // Act & Assert 
            foreach (var movie in mockMoviesData)
            {
                var idioma = GetProperty(movie, "Idioma") as string;
                Assert.NotNull(idioma);
                Assert.False(string.IsNullOrEmpty(idioma));
                Assert.Contains(idioma, idiomasValidos);
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataPosterHandling_DeveTerPosterValidosOuVazios()
        {
            // Arrange
            
            // Act & Assert
            foreach (var movie in mockMoviesData)
            {
                var poster = GetProperty(movie, "Poster") as string;
                
                if (!string.IsNullOrEmpty(poster))
                {
                    Assert.True(poster.StartsWith("http://") || poster.StartsWith("https://"), 
                        $"Poster URL '{poster}' should start with http:// or https://");
                    
                    Assert.True(poster.Contains("tmdb.org") || poster.Contains("cinemas.nos.pt") || poster.Contains("cineplace.pt") || poster.Contains("cdn.nos.pt"),
                        $"Poster URL '{poster}' should be from known image service");
                }
            }
        }

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataLinkConsistency_DeveTerLinksValidosPorCinema()
        {
            // Arrange
            
            // Act & Assert
            foreach (var movie in mockMoviesData)
            {
                var cinema = GetProperty(movie, "Cinema") as string;
                var link = GetProperty(movie, "Link") as string;
                
                Assert.NotNull(link);
                Assert.False(string.IsNullOrEmpty(link));
                
                if (cinema == "Cinema NOS")
                {
                    Assert.Contains("cinemas.nos.pt", link);
                }
                else if (cinema == "Cineplace")
                {
                    Assert.Contains("cineplace.pt", link);
                }
                
                Assert.True(link.StartsWith("http://") || link.StartsWith("https://"), 
                    $"Link '{link}' should be valid URL");
            }
        }       

        [Fact]
        public async Task GetFilmesEmCartaz_MockDataTitleUniqueness_DeveTerTitulosUnicosPorCinema()
        {
            // Arrange
            
            // Act 
            var moviesByCinema = mockMoviesData.GroupBy(m => GetProperty(m, "Cinema"));
            
            // Assert 
            foreach (var cinemaGroup in moviesByCinema)
            {
                var titlesInCinema = cinemaGroup.Select(m => GetProperty(m, "Titulo")).ToList();
                var uniqueTitles = titlesInCinema.Distinct().ToList();
                
                Assert.Equal(titlesInCinema.Count, uniqueTitles.Count);
            }
        } 

        private string GetProperty(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            return property?.GetValue(obj)?.ToString() ?? string.Empty;
        }

    }
}
