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

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class NotificacoesErrorHandlingTests
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

        private static NotificacoesController CreateControllerWithoutAuth(FilmAholicDbContext context, IMovieService movieService)
        {
            var controller = new NotificacoesController(context, movieService);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
            return controller;
        }

        #region Authentication Error Tests

        [Fact]
        public async Task GetPreferenciasNotificacao_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("AuthPrefs");
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, mockMovieService.Object);

                // Act
                var result = await controller.GetPreferenciasNotificacao();

                // Assert
                Assert.IsType<UnauthorizedResult>(result.Result);
            }
        }

        [Fact]
        public async Task PutPreferenciasNotificacao_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("AuthPutPrefs");
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, mockMovieService.Object);
                var dto = new NotificacoesController.PreferenciasNotificacaoDto();

                // Act
                var result = await controller.PutPreferenciasNotificacao(dto);

                // Assert
                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        [Fact]
        public async Task GetResumoEstatisticasFeed_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("AuthResumo");
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, mockMovieService.Object);

                // Act
                var result = await controller.GetResumoEstatisticasFeed();

                // Assert
                Assert.IsType<UnauthorizedResult>(result.Result);
            }
        }

        [Fact]
        public async Task GetNotificacoesComunidadeFeed_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("AuthComunidade");
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, mockMovieService.Object);

                // Act
                var result = await controller.GetNotificacoesComunidadeFeed();

                // Assert
                Assert.IsType<UnauthorizedResult>(result.Result);
            }
        }

        #endregion

        #region Invalid Data Error Tests

        [Fact]
        public async Task PutPreferenciasNotificacao_FrequenciaNula_DeveRetornarBadRequest()
        {
            // Arrange
            var options = GetDbOptions("FrequenciaNula");
            var userId = "user-freq";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);
                var dto = new NotificacoesController.PreferenciasNotificacaoDto
                {
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = null
                };

                // Act
                var result = await controller.PutPreferenciasNotificacao(dto);

                // Assert - Frequência null é tratada como string vazia
                Assert.IsType<BadRequestObjectResult>(result);
            }
        }

        [Fact]
        public async Task MarcarResumoEstatisticasComoLida_IdInexistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = GetDbOptions("IdInexistente");
            var userId = "user-id-inv";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.MarcarResumoEstatisticasComoLida(99999);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task MarcarResumoEstatisticasComoLida_NotificacaoDeOutroTipo_DeveRetornarNotFound()
        {
            // Arrange
            var options = GetDbOptions("TipoErrado");
            var userId = "user-tipo";
            var mockMovieService = new Mock<IMovieService>();
            int notifId;

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar notificação de outro tipo
                var notif = new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "NovaEstreia", // Tipo diferente
                    CriadaEm = DateTime.UtcNow.AddHours(-1)
                };
                context.Notificacoes.Add(notif);
                await context.SaveChangesAsync();
                notifId = notif.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Tentar marcar como lida uma notificação de tipo diferente
                var result = await controller.MarcarResumoEstatisticasComoLida(notifId);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        #endregion

        #region Marcar Todas Como Lidas Error Tests

        [Fact]
        public async Task MarcarTodasComunidadeComoLidas_SemNotificacoes_NaoDeveFalhar()
        {
            // Arrange
            var options = GetDbOptions("SemNotificacoes");
            var userId = "user-sem-notifs";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Não há notificações para marcar
                var result = await controller.MarcarTodasNotificacoesComunidadeComoLidas();

                // Assert - Não deve falhar
                Assert.IsType<NoContentResult>(result);
            }
        }

        [Fact]
        public async Task MarcarTodasMedalhasComoLidas_SemNotificacoes_NaoDeveFalhar()
        {
            // Arrange
            var options = GetDbOptions("SemMedalhas");
            var userId = "user-sem-medalhas";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.MarcarTodasNotificacoesMedalhaComoLidas();

                // Assert
                Assert.IsType<NoContentResult>(result);
            }
        }

        #endregion

        #region Corpo JSON Parsing Error Tests

        [Fact]
        public async Task GetResumoEstatisticasFeed_CorpoJSONInvalido_NaoDeveFalhar()
        {
            // Arrange
            var options = GetDbOptions("JSONInvalido");
            var userId = "user-json";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    ResumoEstatisticasAtiva = true
                });

                // Notificação com JSON inválido
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "{\"tempoTotalHoras\": \"invalido\"}", // valor deveria ser número
                    CriadaEm = DateTime.UtcNow
                });

                // Notificação com JSON quebrado
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "{broken json",
                    CriadaEm = DateTime.UtcNow.AddMinutes(-1)
                });

                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetResumoEstatisticasFeed();

                // Assert - Não deve falhar, apenas retornar corpo null
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.ResumoEstatisticasFeedDto>(okResult.Value);
                Assert.Equal(2, dto.Unread.Count);
            }
        }

        [Fact]
        public async Task GetNotificacoesMedalhaFeed_CorpoJSONInvalido_NaoDeveFalhar()
        {
            // Arrange
            var options = GetDbOptions("MedalhaJSONInvalido");
            var userId = "user-medalha-json";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                // Medalha com corpo null
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = null,
                    CriadaEm = DateTime.UtcNow
                });

                // Medalha com corpo vazio
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = "",
                    CriadaEm = DateTime.UtcNow.AddMinutes(-1)
                });

                // Medalha com corpo inválido
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = "not json at all",
                    CriadaEm = DateTime.UtcNow.AddMinutes(-2)
                });

                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetNotificacoesMedalhaFeed();

                // Assert - Não deve falhar, apenas retornar valores padrão
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.NotificacaoMedalhaFeedDto>(okResult.Value);
                Assert.Equal(3, dto.Unread.Count);
            }
        }

        #endregion

        #region Service Unavailable Error Tests

        [Fact]
        public async Task GetNovaEstreia_ComPreferenciasDesativadas_DeveRetornarListaVazia()
        {
            // Arrange
            var options = GetDbOptions("Desativada");
            var userId = "user-desativada";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = false // Desativado
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetNovaEstreia();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var filmes = Assert.IsType<List<Filme>>(okResult.Value);
                Assert.Empty(filmes);
            }
        }

        [Fact]
        public async Task GetResumoEstatisticasFeed_ComPreferenciasDesativadas_DeveRetornarVazio()
        {
            // Arrange
            var options = GetDbOptions("ResumoDesativado");
            var userId = "user-resumo-off";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    ResumoEstatisticasAtiva = false
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.GetResumoEstatisticasFeed();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.ResumoEstatisticasFeedDto>(okResult.Value);
                Assert.Empty(dto.Unread);
                Assert.Empty(dto.Read);
            }
        }

        #endregion

        #region Concurrency Error Tests

        [Fact]
        public async Task MarcarComoLida_Concorrencia_NaoDeveDuplicar()
        {
            // Arrange
            var options = GetDbOptions("Concorrencia");
            var userId = "user-concorrencia";
            var mockMovieService = new Mock<IMovieService>();
            int notifId;

            using (var context = new FilmAholicDbContext(options))
            {
                var notif = new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    CriadaEm = DateTime.UtcNow.AddHours(-1)
                };
                context.Notificacoes.Add(notif);
                await context.SaveChangesAsync();
                notifId = notif.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Marcar duas vezes
                var result1 = await controller.MarcarResumoEstatisticasComoLida(notifId);
                var result2 = await controller.MarcarResumoEstatisticasComoLida(notifId);

                // Assert - Ambas devem retornar sucesso (idempotente)
                Assert.IsType<NoContentResult>(result1);
                Assert.IsType<NoContentResult>(result2);

                var notif = await context.Notificacoes.FindAsync(notifId);
                Assert.NotNull(notif.LidaEm);
            }
        }

        #endregion
    }
}
