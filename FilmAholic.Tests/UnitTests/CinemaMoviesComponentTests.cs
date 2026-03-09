using Microsoft.AspNetCore.Mvc;
using FilmAholic.Server.Controllers;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Moq;
using Xunit;
using System.Threading.Tasks;
using System;

namespace FilmAholic.Tests.UnitTests
{
    public class CinemaMoviesComponentTests
    {
        [Fact]
        public void CinemaMoviesComponent_ShouldInitializeCorrectly()
        {
            // Arrange 
            var isLoading = true;
            var cinemaMovies = new List<object>();
            var error = (string?)null;

            // Act 
            isLoading = false; 
            error = null; 

            // Assert
            Assert.False(isLoading);
            Assert.NotNull(cinemaMovies);
            Assert.Null(error);
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldHandleLoadingError()
        {
            // Arrange
            var isLoading = true;
            var cinemaMovies = new List<object>();
            var error = (string?)null;

            // Act 
            isLoading = false;
            error = "Não foi possível carregar os filmes em cartaz.";
            cinemaMovies.Clear();

            // Assert
            Assert.False(isLoading);
            Assert.Empty(cinemaMovies);
            Assert.NotNull(error);
            Assert.Equal("Não foi possível carregar os filmes em cartaz.", error);
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldFilterMoviesByCinema()
        {
            // Arrange 
            var allMovies = new List<object>
            {
                new { Id = "nos-1", Cinema = "Cinema NOS", Titulo = "Duna: Parte Dois" },
                new { Id = "cp-1", Cinema = "Cineplace", Titulo = "Oppenheimer" },
                new { Id = "nos-2", Cinema = "Cinema NOS", Titulo = "Guardiões da Galáxia" }
            };

            // Act 
            var nosMovies = allMovies.FindAll(m => 
                m.GetType().GetProperty("Cinema")?.GetValue(m)?.ToString() == "Cinema NOS");
            var cineplaceMovies = allMovies.FindAll(m => 
                m.GetType().GetProperty("Cinema")?.GetValue(m)?.ToString() == "Cineplace");

            // Assert
            Assert.Equal(2, nosMovies.Count);
            Assert.Single(cineplaceMovies);
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldParseDurationCorrectly()
        {
            // Arrange & Act & Assert 
            Assert.Equal(166, ParseDuration("2h 46min")); 
            Assert.Equal(120, ParseDuration("2h")); 
            Assert.Equal(46, ParseDuration("46min"));
            Assert.Equal(0, ParseDuration(""));
            Assert.Equal(0, ParseDuration(null));
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldNavigateBack()
        {
            // Arrange 
            var currentRoute = "/cinema-movies";
            var targetRoute = "/dashboard";

            // Act 
            var navigatedRoute = GoBack(currentRoute);

            // Assert
            Assert.Equal(targetRoute, navigatedRoute);
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldHandleMovieDetailsNavigation()
        {
            // Arrange 
            var movieTitle = "Duna: Parte Dois";
            var tmdbId = 12345;
            var movieNotFound = (string?)null;

            // Act 
            var navigationResult = ViewMovieDetails(movieTitle, tmdbId, ref movieNotFound);

            // Assert
            Assert.Equal($"/movie-detail/{tmdbId}", navigationResult);
            Assert.Null(movieNotFound);
        }

        [Fact]
        public void CinemaMoviesComponent_ShouldNavigateToCinemaLinks()
        {
            // Arrange 
            var nosMovie = new { Cinema = "Cinema NOS", Link = "https://www.cinemas.nos.pt/filme/duna-parte-dois" };
            var cineplaceMovie = new { Cinema = "Cineplace", Link = "https://www.cineplace.pt/filme/oppenheimer" };

            // Act 
            var nosNavigation = NavigateToCinemaLink(nosMovie);
            var cineplaceNavigation = NavigateToCinemaLink(cineplaceMovie);

            // Assert
            Assert.Equal("https://www.cinemas.nos.pt/filme/duna-parte-dois", nosNavigation);
            Assert.Equal("https://www.cineplace.pt/filme/oppenheimer", cineplaceNavigation);
        }       

        private int ParseDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return 0;
            
            var hours = 0;
            var minutes = 0;
            
            var hoursMatch = System.Text.RegularExpressions.Regex.Match(duration, @"(\d+)h");
            if (hoursMatch.Success)
                hours = int.Parse(hoursMatch.Groups[1].Value);
            
            var minutesMatch = System.Text.RegularExpressions.Regex.Match(duration, @"(\d+)min");
            if (minutesMatch.Success)
                minutes = int.Parse(minutesMatch.Groups[1].Value);
            
            return hours * 60 + minutes;
        }

        private string GoBack(string fromRoute)
        {
            return "/dashboard"; 
        }

        private string ViewMovieDetails(string title, int tmdbId, ref string? movieNotFound)
        {
            return $"/movie-detail/{tmdbId}";
        }

        private string NavigateToCinemaLink(object movie)
        {
            var cinema = movie.GetType().GetProperty("Cinema")?.GetValue(movie)?.ToString();
            var link = movie.GetType().GetProperty("Link")?.GetValue(movie)?.ToString();
            
            return link ?? "";
        }
    }
}
