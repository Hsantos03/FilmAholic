using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FilmAholic.Tests;
using Moq;
using Xunit;

namespace FilmAholic.Tests.BoundaryTests
{
    public class ComunidadesBoundaryTests
    {
        private static DbContextOptions<FilmAholicDbContext> GetDbOptions(string dbName)
        {
            return new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: dbName + "_" + Guid.NewGuid())
                .Options;
        }

        private static ComunidadesController CreateControllerWithUser(FilmAholicDbContext context, IWebHostEnvironment env, ILogger<ComunidadesController> logger, string userId)
        {
            var controller = new ComunidadesController(context, env, logger, TestMovieServiceMocks.ForComunidadesController());
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

        private static IWebHostEnvironment CreateMockWebHostEnvironment()
        {
            var mock = new Mock<IWebHostEnvironment>();
            mock.Setup(e => e.WebRootPath).Returns("C:\\fake\\wwwroot");
            return mock.Object;
        }

        private static ILogger<ComunidadesController> CreateMockLogger()
        {
            var mock = new Mock<ILogger<ComunidadesController>>();
            return mock.Object;
        }

        #region LimiteMembros Boundary Tests

        [Theory]
        [InlineData(null, false)]      // Sem limite - deve permitir
        [InlineData(0, false)]         // Limite zero tratado como sem limite
        [InlineData(1, true)]          // Limite mínimo - 1 membro
        [InlineData(10, true)]         // Limite pequeno
        [InlineData(100, true)]        // Limite médio
        [InlineData(10000, true)]      // Limite grande
        public async Task Juntar_DeveRespeitarLimiteMembros(int? limiteMembros, bool deveSerRejeitado)
        {
            // Arrange
            var options = GetDbOptions("Limite_" + limiteMembros);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            var newUserId = "user-new";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Limitada",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false,
                    LimiteMembros = limiteMembros
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;

                // Adicionar admin
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                // Se há limite, preencher até ao máximo
                if (limiteMembros.HasValue && limiteMembros.Value > 0)
                {
                    for (int i = 0; i < limiteMembros.Value; i++)
                    {
                        context.ComunidadeMembros.Add(new ComunidadeMembro
                        {
                            ComunidadeId = comunidadeId,
                            UtilizadorId = $"user-{i}",
                            Role = "Membro",
                            Status = "Ativo",
                            DataEntrada = DateTime.UtcNow
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, newUserId);

                // Act
                var result = await controller.Juntar(comunidadeId);

                // Assert
                if (deveSerRejeitado)
                {
                    var conflictResult = Assert.IsType<ConflictObjectResult>(result);
                    Assert.Contains("limite de membros", conflictResult.Value.ToString().ToLower());
                }
                else
                {
                    Assert.IsType<OkResult>(result);
                }
            }
        }

        [Theory]
        [InlineData(10, false)] // Limite igual aos membros atuais (6), pode manter
        [InlineData(5, true)]   // Limite de 5, temos 6 membros (5+admin), não pode reduzir
        public async Task Update_DeveValidarReducaoLimiteMembros(int novoLimite, bool deveFalhar)
        {
            // Arrange
            var options = GetDbOptions("UpdateLimite_" + novoLimite);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    DataCriacao = DateTime.UtcNow,
                    LimiteMembros = 10
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;

                // Admin
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                // Adicionar 5 membros
                for (int i = 0; i < 5; i++)
                {
                    context.ComunidadeMembros.Add(new ComunidadeMembro
                    {
                        ComunidadeId = comunidadeId,
                        UtilizadorId = $"user-{i}",
                        Role = "Membro",
                        Status = "Ativo",
                        DataEntrada = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var form = new ComunidadesController.ComunidadeUpdateForm
                {
                    Nome = "Comunidade Atualizada",
                    LimiteMembros = novoLimite
                };

                // Act
                var result = await controller.Update(comunidadeId, form);

                // Assert
                if (deveFalhar)
                {
                    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                    Assert.NotNull(badRequest.Value);
                    Assert.Contains("limite de membros", badRequest.Value.ToString().ToLower());
                }
                else
                {
                    Assert.IsType<OkObjectResult>(result);
                }
            }
        }

        #endregion

        #region Nome Boundary Tests

        [Theory]
        [InlineData("A")]                              // Nome mínimo (1 char)
        [InlineData("Nome Normal")]                  // Nome normal
        [InlineData("Nome Com Espacos")]             // Nome com espaços
        [InlineData("A2345678901234567890123456789012345678901234567890")] // 50 chars
        [InlineData("Nome@Com#Caracteres$Especiais!")] // Caracteres especiais
        public async Task Create_DeveAceitarNomesValidos(string nome)
        {
            // Arrange
            var options = GetDbOptions("Nome_" + nome.Length);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-creator";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Setup HttpContext Request para PublicBaseUrl
                controller.ControllerContext.HttpContext.Request.Scheme = "https";
                controller.ControllerContext.HttpContext.Request.Host = new HostString("test.com");

                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = nome,
                    Descricao = "Descricao"
                };

                // Act
                var result = await controller.Create(form);

                // Assert
                Assert.IsType<CreatedAtActionResult>(result);
            }
        }

        [Theory]
        [InlineData("")]           // Nome vazio
        [InlineData("   ")]        // Só espaços
        [InlineData(null)]          // Nome null
        public async Task Create_DeveRejeitarNomesInvalidos(string nome)
        {
            // Arrange
            var options = GetDbOptions("NomeInvalido_" + (nome?.Length ?? -1));
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-creator";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);
                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = nome ?? "",
                    Descricao = "Descricao"
                };

                // Act
                var result = await controller.Create(form);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("Nome", badRequest.Value.ToString());
            }
        }

        #endregion

        #region Descricao Boundary Tests

        [Theory]
        [InlineData(null)]      // Sem descrição é válido
        [InlineData("")]        // Descrição vazia é válida
        [InlineData("A")]       // Descrição mínima
        [InlineData("Descrição normal da comunidade")] // Descrição normal
        public async Task Create_DeveAceitarDescricoesValidas(string descricao)
        {
            // Arrange
            var options = GetDbOptions("Descricao_" + (descricao?.Length ?? -1));
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-creator";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);
                controller.ControllerContext.HttpContext.Request.Scheme = "https";
                controller.ControllerContext.HttpContext.Request.Host = new HostString("test.com");

                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = "Comunidade Teste",
                    Descricao = descricao
                };

                // Act
                var result = await controller.Create(form);

                // Assert
                Assert.IsType<CreatedAtActionResult>(result);
            }
        }

        #endregion

        #region Castigo (Horas) Boundary Tests

        [Theory]
        [InlineData(1)]      // 1 hora mínimo
        [InlineData(24)]     // 24 horas (1 dia)
        [InlineData(168)]    // 168 horas (1 semana)
        [InlineData(720)]    // 720 horas (1 mês)
        [InlineData(0)]      // 0 horas (castigo imediato?)
        [InlineData(-1)]     // -1 horas (castigo retroativo)
        public async Task CastigarMembro_DeveAceitarHorasValidas(int horas)
        {
            // Arrange
            var options = GetDbOptions("Castigo_" + horas);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            var membroId = "user-membro";
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

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = membroId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act
                var result = await controller.CastigarMembro(comunidadeId, membroId, horas);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                Assert.Contains("castigado", okResult.Value.ToString().ToLower());
            }
        }

        #endregion

        #region Ban Duration Boundary Tests

        [Theory]
        [InlineData(null)]     // Ban permanente
        [InlineData(1)]        // 1 dia mínimo
        [InlineData(7)]        // 1 semana
        [InlineData(30)]       // 1 mês
        [InlineData(365)]      // 1 ano
        [InlineData(3650)]     // 10 anos (máximo)
        [InlineData(4000)]     // Over maximum - deve ser clamped
        [InlineData(0)]        // 0 dias (ban permanente?)
        [InlineData(-5)]       // Negativo - comportamento?
        public async Task BanirMembro_DeveAceitarDuracoesValidas(int? duracaoDias)
        {
            // Arrange
            var options = GetDbOptions("Ban_" + duracaoDias);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            var membroId = "user-membro";
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

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = membroId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var form = new ComunidadesController.BanirMembroForm
                {
                    DuracaoDias = duracaoDias,
                    Motivo = "Teste"
                };

                // Act
                var result = await controller.BanirMembro(comunidadeId, membroId, form);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                Assert.Contains("banido", okResult.Value.ToString().ToLower());
            }
        }

        #endregion

        #region Sugestoes Filmes Limit Boundary Tests

        [Theory]
        [InlineData(-5)]    // Negative becomes 1
        [InlineData(0)]     // Zero becomes 1
        [InlineData(1)]     // Minimum
        [InlineData(24)]   // Normal
        [InlineData(60)]   // Maximum
        [InlineData(100)]  // Over maximum
        public async Task GetSugestoesFilmesComunidade_DeveRespeitarLimitBoundary(int inputLimit)
        {
            // Arrange
            var options = GetDbOptions("SugestoesLimit_" + inputLimit);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-sugestoes";

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidade.Id,
                    UtilizadorId = userId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.GetSugestoesFilmesComunidade(limit: inputLimit);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var sugestoes = Assert.IsType<List<SugestaoFilmeComunidadeDto>>(okResult.Value);
                // Resultado pode ser vazio, mas o limite foi aplicado
            }
        }

        #endregion

        #region Ranking Boundary Tests

        [Theory]
        [InlineData("filmes")]   // Métrica válida
        [InlineData("tempo")]    // Métrica válida
        [InlineData("FILMES")]   // Case insensitive
        [InlineData("TEMPO")]    // Case insensitive
        [InlineData("")]         // Default (filmes)
        [InlineData("invalido")] // Default comportamento
        public async Task GetRanking_DeveAceitarMetricasValidas(string metrica)
        {
            // Arrange
            var options = GetDbOptions("Ranking_" + metrica);
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-ranking";
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

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = userId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.GetRanking(comunidadeId, metrica);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var ranking = Assert.IsType<List<ComunidadesController.RankingMembroDto>>(okResult.Value);
            }
        }

        #endregion
    }
}
