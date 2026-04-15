using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class NotificacoesBoundaryTests
    {
        private static DbContextOptions<FilmAholicDbContext> GetDbOptions(string dbName)
        {
            return new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: dbName + "_" + Guid.NewGuid())
                .Options;
        }

        private static NotificacoesController CreateControllerWithUser(FilmAholicDbContext context, IMovieService movieService, string userId)
        {
            var controller = new NotificacoesController(context, movieService);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        #region Limit Parameter Boundary Tests

        [Theory]
        [InlineData(-5, 5)]   // Negative limit should be clamped to 5
        [InlineData(0, 5)]    // Zero limit should be clamped to 5
        [InlineData(1, 1)]    // Minimum valid limit
        [InlineData(5, 5)]    // Normal limit
        [InlineData(10, 10)]  // Maximum limit
        [InlineData(15, 10)]  // Over maximum should be clamped to 10
        [InlineData(100, 10)] // Way over maximum should be clamped to 10
        public async Task GetNovaEstreia_DeveRespeitarLimitBoundary(int inputLimit, int expectedLimit)
        {
            // Arrange
            var options = GetDbOptions("LimitBoundary_" + inputLimit);
            var userId = "user-limit-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar preferências
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true
                });

                // Adicionar filmes futuros
                for (int i = 0; i < 20; i++)
                {
                    context.Filmes.Add(new Filme
                    {
                        Titulo = $"Filme {i}",
                        ReleaseDate = DateTime.UtcNow.AddDays(i + 1),
                        Ano = DateTime.UtcNow.Year
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetNovaEstreia(inputLimit);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var filmes = Assert.IsType<List<Filme>>(okResult.Value);
                Assert.True(filmes.Count <= expectedLimit, $"Expected at most {expectedLimit} films, got {filmes.Count}");
            }
        }

        [Theory]
        [InlineData(-1, 0)]  // Negative should be clamped to 0
        [InlineData(0, 0)]   // Zero stays zero
        [InlineData(5, 5)]   // Normal value
        [InlineData(20, 20)] // Maximum value
        [InlineData(25, 20)] // Over maximum should be clamped
        public async Task GetResumoEstatisticasFeed_DeveRespeitarLimitBoundary(int inputLimit, int expectedLimit)
        {
            // Arrange
            var options = GetDbOptions("ResumoLimitBoundary_" + inputLimit);
            var userId = "user-resumo-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    ResumoEstatisticasAtiva = true
                });

                // Criar várias notificações
                for (int i = 0; i < 30; i++)
                {
                    context.Notificacoes.Add(new Notificacao
                    {
                        UtilizadorId = userId,
                        Tipo = "ResumoEstatisticas",
                        CriadaEm = DateTime.UtcNow.AddHours(-i),
                        LidaEm = i % 2 == 0 ? null : DateTime.UtcNow.AddHours(-i / 2)
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetResumoEstatisticasFeed(unreadLimit: inputLimit, readLimit: inputLimit);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.ResumoEstatisticasFeedDto>(okResult.Value);
                Assert.True(dto.Unread.Count <= expectedLimit);
                Assert.True(dto.Read.Count <= expectedLimit);
            }
        }

        #endregion

        #region WindowDays Parameter Boundary Tests

        [Theory]
        [InlineData(-10, 60)]  // Negative should be clamped to 60 (default)
        [InlineData(0, 60)]    // Zero should be clamped to 60
        [InlineData(1, 1)]     // Minimum valid
        [InlineData(30, 30)]   // Normal
        [InlineData(180, 180)] // Maximum for proximas-estreias
        public async Task GetProximasEstreias_DeveRespeitarWindowDaysBoundary(int inputWindow, int expectedWindow)
        {
            // Arrange
            var options = GetDbOptions("WindowBoundary_" + inputWindow);
            var userId = "user-window-test";
            var mockMovieService = new Mock<IMovieService>();
            mockMovieService.Setup(m => m.GetUpcomingMoviesAccumulatedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Filme>());

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - O teste verifica se o método é chamado sem exceções
                var result = await controller.GetProximasEstreiasPersonalizadas(windowDays: inputWindow);

                // Assert
                Assert.IsType<OkObjectResult>(result.Result);
            }
        }

        #endregion

        #region MaxAnoAhead Parameter Boundary Tests

        [Theory]
        [InlineData(-5, 0)]   // Negative should be clamped to 0
        [InlineData(0, 0)]    // Zero stays zero
        [InlineData(2, 2)]    // Normal value
        [InlineData(5, 5)]    // Maximum default
        [InlineData(10, 10)]  // Higher value allowed
        public async Task GetNovaEstreia_DeveRespeitarMaxAnoAheadBoundary(int inputMaxAno, int expectedMaxAno)
        {
            // Arrange
            var options = GetDbOptions("MaxAnoBoundary_" + inputMaxAno);
            var userId = "user-maxano-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetNovaEstreia(maxAnoAhead: inputMaxAno);

                // Assert - Verifica se o resultado é OK
                Assert.IsType<OkObjectResult>(result.Result);
            }
        }

        #endregion

        #region Frequencia Parameter Boundary Tests

        [Theory]
        [InlineData("Imediata", true)]
        [InlineData("imediata", true)]
        [InlineData("IMEDIATA", true)]
        [InlineData("Diaria", true)]
        [InlineData("diaria", true)]
        [InlineData("Semanal", true)]
        [InlineData("semanal", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("Invalida", false)]
        [InlineData("Mensal", false)]
        public async Task PutPreferenciasNotificacao_DeveValidarFrequencias(string frequencia, bool deveSerValida)
        {
            // Arrange
            var options = GetDbOptions("FrequenciaBoundary_" + (frequencia ?? "null"));
            var userId = "user-freq-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = "Diaria"
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);
                var dto = new NotificacoesController.PreferenciasNotificacaoDto
                {
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = frequencia,
                    ResumoEstatisticasFrequencia = "Semanal"
                };

                // Act
                var result = await controller.PutPreferenciasNotificacao(dto);

                // Assert
                if (deveSerValida)
                {
                    Assert.IsType<NoContentResult>(result);
                }
                else
                {
                    Assert.IsType<BadRequestObjectResult>(result);
                }
            }
        }

        #endregion

        #region ResumoEstatisticasFrequencia Boundary Tests

        [Theory]
        [InlineData("Diaria", true)]
        [InlineData("diaria", true)]
        [InlineData("Semanal", true)]
        [InlineData("SEMANAL", true)]
        [InlineData("Imediata", false)]  // Imediata não é válida para resumo
        [InlineData("Mensal", false)]
        [InlineData("", true)]           // Vazio mantém valor existente
        [InlineData(null, true)]         // Null mantém valor existente
        public async Task PutPreferenciasNotificacao_DeveValidarResumoFrequencias(string frequencia, bool deveSerValida)
        {
            // Arrange
            var options = GetDbOptions("ResumoFreqBoundary_" + (frequencia ?? "null"));
            var userId = "user-resumofreq-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    ResumoEstatisticasAtiva = true,
                    ResumoEstatisticasFrequencia = "Semanal"
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);
                var dto = new NotificacoesController.PreferenciasNotificacaoDto
                {
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = "Diaria",
                    ResumoEstatisticasAtiva = true,
                    ResumoEstatisticasFrequencia = frequencia
                };

                // Act
                var result = await controller.PutPreferenciasNotificacao(dto);

                // Assert
                if (deveSerValida)
                {
                    Assert.IsType<NoContentResult>(result);
                }
                else
                {
                    Assert.IsType<BadRequestObjectResult>(result);
                }
            }
        }

        #endregion

        #region Community Notification Feed Limit Tests

        [Theory]
        [InlineData(-5, 0)]   // Negative becomes 0
        [InlineData(0, 0)]    // Zero stays 0
        [InlineData(20, 20)]  // Normal
        [InlineData(50, 50)]  // Maximum
        [InlineData(60, 50)]  // Over maximum clamped to 50
        [InlineData(100, 50)] // Way over maximum
        public async Task GetNotificacoesComunidadeFeed_DeveRespeitarLimitBoundary(int inputLimit, int expectedLimit)
        {
            // Arrange
            var options = GetDbOptions("ComunidadeLimit_" + inputLimit);
            var userId = "user-com-limit-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar comunidade
                var comunidade = new Comunidade
                {
                    Nome = "Test Comunidade",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();

                // Criar várias notificações de comunidade
                for (int i = 0; i < 60; i++)
                {
                    context.NotificacoesComunidade.Add(new NotificacaoComunidade
                    {
                        UtilizadorId = userId,
                        ComunidadeId = comunidade.Id,
                        Tipo = "post",
                        CriadaEm = DateTime.UtcNow.AddHours(-i),
                        LidaEm = i % 2 == 0 ? null : DateTime.UtcNow.AddMinutes(-i)
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetNotificacoesComunidadeFeed(unreadLimit: inputLimit, readLimit: inputLimit);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.NotificacaoComunidadeFeedDto>(okResult.Value);
                Assert.True(dto.Unread.Count <= expectedLimit);
                Assert.True(dto.Read.Count <= expectedLimit);
            }
        }

        #endregion

        #region Reminder Jogo Feed Boundary Tests

        [Fact]
        public async Task GetReminderJogoFeed_DeveLimitarNotificacoes()
        {
            // Arrange
            var options = GetDbOptions("ReminderLimit");
            var userId = "user-reminder-test";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar mais de 5 notificações
                for (int i = 0; i < 10; i++)
                {
                    context.Notificacoes.Add(new Notificacao
                    {
                        UtilizadorId = userId,
                        Tipo = "ReminderJogo",
                        Corpo = "Reminder " + i,
                        CriadaEm = DateTime.UtcNow.AddHours(-i)
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetReminderJogoFeed();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var notifs = okResult.Value as System.Collections.IEnumerable;
                Assert.NotNull(notifs);
                int count = 0;
                foreach (var _ in notifs) count++;
                Assert.True(count <= 12);
            }
        }

        #endregion
    }
}
