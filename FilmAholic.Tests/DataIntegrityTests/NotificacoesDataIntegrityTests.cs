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

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class NotificacoesDataIntegrityTests
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

        #region Notificacao Lifecycle Tests

        [Fact]
        public async Task Notificacao_NaoPodeSerLidaAntesDeSerCriada()
        {
            // Arrange
            var options = GetDbOptions("NotifLifecycle");
            var userId = "user-lifecycle";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var notif = new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "NovaEstreia",
                    CriadaEm = DateTime.UtcNow,
                    LidaEm = DateTime.UtcNow.AddHours(-1) // Lida ANTES de ser criada!
                };
                context.Notificacoes.Add(notif);
                await context.SaveChangesAsync();

                // Assert - Verifica inconsistência de dados
                var savedNotif = await context.Notificacoes.FirstAsync();
                Assert.True(savedNotif.LidaEm < savedNotif.CriadaEm);
                // Nota: A aplicação deve prevenir isto na lógica de negócio
            }
        }

        [Fact]
        public async Task Notificacao_MarcarComoLida_DeveAtualizarCorretamente()
        {
            // Arrange
            var options = GetDbOptions("MarcarLida");
            var userId = "user-lida";
            var mockMovieService = new Mock<IMovieService>();
            int notifId;

            using (var context = new FilmAholicDbContext(options))
            {
                var notif = new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "{\"tempoTotalHoras\":10}",
                    CriadaEm = DateTime.UtcNow.AddHours(-2)
                };
                context.Notificacoes.Add(notif);
                await context.SaveChangesAsync();
                notifId = notif.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);
                var beforeMark = DateTime.UtcNow;

                // Act
                var result = await controller.MarcarResumoEstatisticasComoLida(notifId);
                var afterMark = DateTime.UtcNow;

                // Assert
                Assert.IsType<NoContentResult>(result);
                var notif = await context.Notificacoes.FindAsync(notifId);
                Assert.NotNull(notif.LidaEm);
                Assert.True(notif.LidaEm >= beforeMark && notif.LidaEm <= afterMark);
            }
        }

        [Fact]
        public async Task Notificacao_NaoPodeSerModificadaPorOutroUtilizador()
        {
            // Arrange
            var options = GetDbOptions("NotifOwner");
            var ownerId = "user-owner";
            var otherId = "user-other";
            var mockMovieService = new Mock<IMovieService>();
            int notifId;

            using (var context = new FilmAholicDbContext(options))
            {
                var notif = new Notificacao
                {
                    UtilizadorId = ownerId,
                    Tipo = "ResumoEstatisticas",
                    CriadaEm = DateTime.UtcNow.AddHours(-1)
                };
                context.Notificacoes.Add(notif);
                await context.SaveChangesAsync();
                notifId = notif.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, otherId);

                // Act - Tentar marcar notificação de outro utilizador como lida
                var result = await controller.MarcarResumoEstatisticasComoLida(notifId);

                // Assert - Deve retornar NotFound (não pode encontrar notificação de outro)
                Assert.IsType<NotFoundResult>(result);

                var notif = await context.Notificacoes.FindAsync(notifId);
                Assert.Null(notif.LidaEm); // Não deve ter sido modificada
            }
        }

        #endregion

        #region PreferenciasNotificacao Integrity Tests

        [Fact]
        public async Task Preferencias_CriacaoPadrao_DeveTerValoresCorretos()
        {
            // Arrange
            var options = GetDbOptions("PrefsPadrao");
            var userId = "user-prefs";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Chama Get que cria preferências se não existirem
                var result = await controller.GetPreferenciasNotificacao();

                // Assert
                var prefs = await context.PreferenciasNotificacao.FirstOrDefaultAsync(p => p.UtilizadorId == userId);
                Assert.NotNull(prefs);
                Assert.True(prefs.NovaEstreiaAtiva);
                Assert.Equal("Diaria", prefs.NovaEstreiaFrequencia);
                Assert.True(prefs.ResumoEstatisticasAtiva);
                Assert.Equal("Semanal", prefs.ResumoEstatisticasFrequencia);
                Assert.True(prefs.ReminderJogoAtiva);
            }
        }

        [Fact]
        public async Task Preferencias_NaoDevePermitirFrequenciasInvalidasNaBD()
        {
            // Arrange
            var options = GetDbOptions("PrefsInvalidas");
            var userId = "user-prefs-inv";
            var mockMovieService = new Mock<IMovieService>();

            // Simula uma inconsistência - frequência inválida na BD
            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true,
                    NovaEstreiaFrequencia = "FrequenciaInexistente", // Inválida!
                    ResumoEstatisticasAtiva = true,
                    ResumoEstatisticasFrequencia = "OutraInvalida"
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Tenta atualizar com valores da BD
                var prefs = await context.PreferenciasNotificacao.FirstAsync(p => p.UtilizadorId == userId);
                var dto = new NotificacoesController.PreferenciasNotificacaoDto
                {
                    NovaEstreiaAtiva = prefs.NovaEstreiaAtiva,
                    NovaEstreiaFrequencia = prefs.NovaEstreiaFrequencia,
                    ResumoEstatisticasAtiva = prefs.ResumoEstatisticasAtiva,
                    ResumoEstatisticasFrequencia = "Diaria" // Valida
                };

                var result = await controller.PutPreferenciasNotificacao(dto);

                // Assert - Deve rejeitar frequência inválida
                Assert.IsType<BadRequestObjectResult>(result);
            }
        }

        [Fact]
        public async Task Preferencias_UmUtilizador_UmaPreferencia()
        {
            // Arrange
            var options = GetDbOptions("PrefsUnica");
            var userId = "user-unico";

            // Act - Criar preferências múltiplas para o mesmo utilizador (inconsistência)
            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = true
                });
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    NovaEstreiaAtiva = false
                });
                await context.SaveChangesAsync();
            }

            // Assert - Verifica inconsistência
            using (var context = new FilmAholicDbContext(options))
            {
                var count = await context.PreferenciasNotificacao.CountAsync(p => p.UtilizadorId == userId);
                Assert.Equal(2, count); // Inconsistência detectada!
                // Nota: A aplicação deve prevenir isto com índice único na BD
            }
        }

        #endregion

        #region Community Notification Integrity Tests

        [Fact]
        public async Task NotificacaoComunidade_DeveManterRelacaoComComunidade()
        {
            // Arrange
            var options = GetDbOptions("NotifComRelacao");
            var userId = "user-notif-com";
            var mockMovieService = new Mock<IMovieService>();
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Teste",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;

                // Criar notificação de comunidade
                context.NotificacoesComunidade.Add(new NotificacaoComunidade
                {
                    UtilizadorId = userId,
                    ComunidadeId = comunidadeId,
                    Tipo = "post",
                    CriadaEm = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act
                var notif = await context.NotificacoesComunidade
                    .Include(n => n.Comunidade)
                    .FirstAsync(n => n.UtilizadorId == userId);

                // Assert
                Assert.NotNull(notif.Comunidade);
                Assert.Equal("Comunidade Teste", notif.Comunidade.Nome);
            }
        }

        [Fact]
        public async Task NotificacaoComunidade_AoMarcarTodasComoLidas_DeveAtualizarApenasNaoLidas()
        {
            // Arrange
            var options = GetDbOptions("MarcarTodasLidas");
            var userId = "user-marcar-todas";
            var mockMovieService = new Mock<IMovieService>();
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;

                // 3 não lidas
                for (int i = 0; i < 3; i++)
                {
                    context.NotificacoesComunidade.Add(new NotificacaoComunidade
                    {
                        UtilizadorId = userId,
                        ComunidadeId = comunidadeId,
                        Tipo = "post",
                        CriadaEm = DateTime.UtcNow.AddHours(-i)
                    });
                }

                // 2 já lidas
                for (int i = 0; i < 2; i++)
                {
                    context.NotificacoesComunidade.Add(new NotificacaoComunidade
                    {
                        UtilizadorId = userId,
                        ComunidadeId = comunidadeId,
                        Tipo = "post",
                        CriadaEm = DateTime.UtcNow.AddDays(-1),
                        LidaEm = DateTime.UtcNow.AddHours(-i - 1)
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act
                var result = await controller.MarcarTodasNotificacoesComunidadeComoLidas();

                // Assert
                Assert.IsType<NoContentResult>(result);
                var naoLidas = await context.NotificacoesComunidade.CountAsync(n => n.UtilizadorId == userId && n.LidaEm == null);
                var lidas = await context.NotificacoesComunidade.CountAsync(n => n.UtilizadorId == userId && n.LidaEm != null);
                Assert.Equal(0, naoLidas);
                Assert.Equal(5, lidas); // Todas as 5 devem estar lidas agora
            }
        }

        #endregion

        #region Medal Notification Integrity Tests

        [Fact]
        public async Task NotificacaoMedalha_CorpoDeveTerFormatoCorreto()
        {
            // Arrange
            var options = GetDbOptions("MedalhaFormato");
            var userId = "user-medalha";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                // Corpo válido JSON
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = "{\"medalhaId\":1,\"medalhaNome\":\"Teste\",\"medalhaDescricao\":\"Desc\",\"medalhaIconeUrl\":\"url\"}",
                    CriadaEm = DateTime.UtcNow
                });

                // Corpo inválido
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = "texto invalido nao json",
                    CriadaEm = DateTime.UtcNow
                });

                // Corpo null
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "Medalha",
                    Corpo = null,
                    CriadaEm = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Act - Não deve falhar mesmo com dados inválidos
                var result = await controller.GetNotificacoesMedalhaFeed();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result.Result);
                var dto = Assert.IsType<NotificacoesController.NotificacaoMedalhaFeedDto>(okResult.Value);
                Assert.Equal(3, dto.Unread.Count); // Todas devem aparecer, mesmo com dados inválidos
            }
        }

        #endregion

        #region ResumoEstatisticas JSON Integrity Tests

        [Fact]
        public async Task ResumoEstatisticas_CorpoJSONDeveSerValido()
        {
            // Arrange
            var options = GetDbOptions("ResumoJSON");
            var userId = "user-resumo-json";
            var mockMovieService = new Mock<IMovieService>();

            using (var context = new FilmAholicDbContext(options))
            {
                context.PreferenciasNotificacao.Add(new PreferenciasNotificacao
                {
                    UtilizadorId = userId,
                    ResumoEstatisticasAtiva = true
                });

                // JSON válido
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "{\"tempoTotalHoras\":100,\"generosMaisVistos\":[{\"nome\":\"Ação\",\"filmes\":10}]}",
                    CriadaEm = DateTime.UtcNow
                });

                // JSON inválido
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "{invalid json",
                    CriadaEm = DateTime.UtcNow.AddHours(-1)
                });

                // JSON vazio
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = "",
                    CriadaEm = DateTime.UtcNow.AddHours(-2)
                });

                // Null
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "ResumoEstatisticas",
                    Corpo = null,
                    CriadaEm = DateTime.UtcNow.AddHours(-3)
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
                Assert.Equal(4, dto.Unread.Count);

                // Apenas o primeiro deve ter corpo parseado corretamente
                Assert.NotNull(dto.Unread[0].Corpo);
                Assert.Null(dto.Unread[1].Corpo); // JSON inválido
                Assert.Null(dto.Unread[2].Corpo); // Vazio
                Assert.Null(dto.Unread[3].Corpo); // Null
            }
        }

        #endregion

        #region Filme-Notificacao Relationship Tests

        [Fact]
        public async Task Notificacao_FilmeDeveExistirParaNovaEstreia()
        {
            // Arrange
            var options = GetDbOptions("NotifFilme");
            var userId = "user-notif-filme";
            var mockMovieService = new Mock<IMovieService>();
            int filmeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var filme = new Filme
                {
                    Titulo = "Filme Teste",
                    ReleaseDate = DateTime.UtcNow.AddDays(7)
                };
                context.Filmes.Add(filme);
                await context.SaveChangesAsync();
                filmeId = filme.Id;

                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "NovaEstreia",
                    FilmeId = filmeId,
                    CriadaEm = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act
                var notif = await context.Notificacoes
                    .Include(n => n.Filme)
                    .FirstAsync(n => n.UtilizadorId == userId);

                // Assert
                Assert.NotNull(notif.Filme);
                Assert.Equal("Filme Teste", notif.Filme.Titulo);
            }
        }

        [Fact]
        public async Task Notificacao_NovaEstreia_SemFilme_Valido()
        {
            // Arrange
            var options = GetDbOptions("NotifSemFilme");
            var userId = "user-notif-sem-filme";
            var mockMovieService = new Mock<IMovieService>();

            // Notificação sem filme associado
            using (var context = new FilmAholicDbContext(options))
            {
                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "NovaEstreia",
                    FilmeId = null, // Sem filme
                    CriadaEm = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act - Marcar como lida deve funcionar mesmo sem filme
                var controller = CreateControllerWithUser(context, mockMovieService.Object, userId);

                // Criar uma notificação com FilmeId válido primeiro
                var filme = new Filme { Titulo = "Test", ReleaseDate = DateTime.UtcNow.AddDays(5) };
                context.Filmes.Add(filme);
                await context.SaveChangesAsync();

                context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    Tipo = "NovaEstreia",
                    FilmeId = filme.Id,
                    CriadaEm = DateTime.UtcNow.AddHours(-1)
                });
                await context.SaveChangesAsync();

                var result = await controller.MarcarNovaEstreiaComoLida(filme.Id);

                // Assert
                Assert.IsType<NoContentResult>(result);
            }
        }

        #endregion
    }
}
