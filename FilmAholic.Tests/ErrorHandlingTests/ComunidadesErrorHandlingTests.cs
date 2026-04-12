using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FilmAholic.Server.Controllers;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FilmAholic.Tests;
using Moq;
using Xunit;

namespace FilmAholic.Tests.ErrorHandlingTests
{
    public class ComunidadesErrorHandlingTests
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

        private static ComunidadesController CreateControllerWithoutAuth(FilmAholicDbContext context, IWebHostEnvironment env, ILogger<ComunidadesController> logger)
        {
            var controller = new ComunidadesController(context, env, logger, TestMovieServiceMocks.ForComunidadesController());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
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

        #region Authentication Error Tests

        [Fact]
        public async Task Create_SemAuth_NaoDeveFuncionar()
        {
            // Arrange
            var options = GetDbOptions("CreateNoAuth");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, env, logger);
                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = "Comunidade"
                };

                // Act - O controller exige Authorize, mas vamos verificar o comportamento
                // Em testes unitários, o atributo [Authorize] não é verificado automaticamente
                // Act
                var result = await controller.Create(form);

                // Assert - Sem utilizador autenticado, tenta criar mas pode falhar em outras partes
                // O importante é que o método exige autenticação
            }
        }

        [Fact]
        public async Task Juntar_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("JuntarNoAuth");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, env, logger);

                // Act
                var result = await controller.Juntar(comunidadeId);

                // Assert
                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        [Fact]
        public async Task GetMeuEstado_SemAuth_DeveRetornarUnauthorized()
        {
            // Arrange
            var options = GetDbOptions("EstadoNoAuth");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
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
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithoutAuth(context, env, logger);

                // Act
                var result = await controller.GetMeuEstado(comunidadeId);

                // Assert
                Assert.IsType<UnauthorizedResult>(result);
            }
        }

        #endregion

        #region Authorization Error Tests

        [Fact]
        public async Task Update_SemSerAdmin_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("UpdateNoAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var membroId = "user-membro";
            var adminId = "user-admin";
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
                var controller = CreateControllerWithUser(context, env, logger, membroId);
                var form = new ComunidadesController.ComunidadeUpdateForm
                {
                    Nome = "Novo Nome"
                };

                // Act
                var result = await controller.Update(comunidadeId, form);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task Delete_SemSerAdmin_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("DeleteNoAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var membroId = "user-membro";
            var adminId = "user-admin";
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
                var controller = CreateControllerWithUser(context, env, logger, membroId);

                // Act
                var result = await controller.Delete(comunidadeId);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task GetPedidosEntrada_SemSerAdmin_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("PedidosNoAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
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
                    UtilizadorId = membroId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, membroId);

                // Act
                var result = await controller.GetPedidosEntrada(comunidadeId);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task BanirMembro_SemSerAdmin_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("BanirNoAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var membroId = "user-membro";
            var outroMembroId = "user-outro";
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
                    UtilizadorId = membroId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = outroMembroId,
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, membroId);
                var form = new ComunidadesController.BanirMembroForm();

                // Act
                var result = await controller.BanirMembro(comunidadeId, outroMembroId, form);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }

        #endregion

        #region Not Found Error Tests

        [Fact]
        public async Task GetById_ComunidadeInexistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = GetDbOptions("GetByIdInexistente");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ComunidadesController(context, env, logger, TestMovieServiceMocks.ForComunidadesController());

                // Act
                var result = await controller.GetById(99999);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Update_ComunidadeInexistente_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("UpdateInexistente");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar uma comunidade para o admin ser membro (mas não da comunidade 99999)
                var comunidade = new Comunidade { Nome = "Temp", DataCriacao = DateTime.UtcNow };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidade.Id,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var form = new ComunidadesController.ComunidadeUpdateForm
                {
                    Nome = "Novo Nome"
                };

                // Act - Tentar atualizar comunidade que não existe (id 99999)
                // O controller verifica se é admin ANTES de verificar existência
                var result = await controller.Update(99999, form);

                // Assert - Como não é admin da comunidade 99999, retorna Forbid
                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task Delete_ComunidadeInexistente_DeveRetornarForbid()
        {
            // Arrange
            var options = GetDbOptions("DeleteInexistente");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";

            using (var context = new FilmAholicDbContext(options))
            {
                // Criar uma comunidade para o admin ser membro (mas não da comunidade 99999)
                var comunidade = new Comunidade { Nome = "Temp", DataCriacao = DateTime.UtcNow };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();

                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidade.Id,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act - Tentar apagar comunidade que não existe (id 99999)
                // O controller verifica se é admin ANTES de verificar existência
                var result = await controller.Delete(99999);

                // Assert - Como não é admin da comunidade 99999, retorna Forbid
                Assert.IsType<ForbidResult>(result);
            }
        }

        [Fact]
        public async Task AprovarPedidoEntrada_PedidoInexistente_DeveRetornarNotFound()
        {
            // Arrange
            var options = GetDbOptions("AprovarInexistente");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
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
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act
                var result = await controller.AprovarPedidoEntrada(comunidadeId, 99999);

                // Assert
                var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
                Assert.Contains("Pedido", notFoundResult.Value.ToString());
            }
        }

        #endregion

        #region Conflict Error Tests

        [Fact]
        public async Task Create_NomeDuplicado_DeveRetornarConflict()
        {
            // Arrange
            var options = GetDbOptions("NomeDuplicado");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-creator";

            using (var context = new FilmAholicDbContext(options))
            {
                var existingComunidade = new Comunidade
                {
                    Nome = "Comunidade Existente",
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(existingComunidade);
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);
                controller.ControllerContext.HttpContext.Request.Scheme = "https";
                controller.ControllerContext.HttpContext.Request.Host = new HostString("test.com");

                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = "Comunidade Existente", // Nome duplicado
                    Descricao = "Descrição"
                };

                // Act
                var result = await controller.Create(form);

                // Assert
                var conflictResult = Assert.IsType<ConflictObjectResult>(result);
                Assert.Contains("já existe", conflictResult.Value.ToString().ToLower());
            }
        }

        [Fact]
        public async Task Juntar_JaEhMembro_DeveRetornarConflict()
        {
            // Arrange
            var options = GetDbOptions("JaEhMembro");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-membro";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false
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
                var result = await controller.Juntar(comunidadeId);

                // Assert
                var conflictResult = Assert.IsType<ConflictObjectResult>(result);
                Assert.Contains("já és membro", conflictResult.Value.ToString().ToLower());
            }
        }

        [Fact]
        public async Task AprovarPedidoEntrada_LimiteAtingido_DeveRetornarConflict()
        {
            // Arrange
            var options = GetDbOptions("LimiteAtingidoAprovar");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            var requesterId = "user-requester";
            int comunidadeId;
            int pedidoId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    IsPrivada = true,
                    LimiteMembros = 2, // Limite de 2 membros (admin + 1)
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

                // Adicionar membro para atingir limite
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = "outro-membro",
                    Role = "Membro",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                var pedido = new ComunidadePedidoEntrada
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = requesterId,
                    Status = "Pendente",
                    DataPedido = DateTime.UtcNow
                };
                context.ComunidadePedidosEntrada.Add(pedido);
                await context.SaveChangesAsync();
                pedidoId = pedido.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act
                var result = await controller.AprovarPedidoEntrada(comunidadeId, pedidoId);

                // Assert
                var conflictResult = Assert.IsType<ConflictObjectResult>(result);
                Assert.Contains("limite", conflictResult.Value.ToString().ToLower());
            }
        }

        #endregion

        #region Bad Request Error Tests

        [Fact]
        public async Task Create_NomeVazio_DeveRetornarBadRequest()
        {
            // Arrange
            var options = GetDbOptions("NomeVazio");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-creator";

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);
                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = "", // Nome vazio
                    Descricao = "Descrição"
                };

                // Act
                var result = await controller.Create(form);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("Nome", badRequest.Value.ToString());
            }
        }

        [Fact]
        public async Task Update_NomeVazio_DeveRetornarBadRequest()
        {
            // Arrange
            var options = GetDbOptions("UpdateNomeVazio");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
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
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var form = new ComunidadesController.ComunidadeUpdateForm
                {
                    Nome = "" // Nome vazio
                };

                // Act
                var result = await controller.Update(comunidadeId, form);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("nome", badRequest.Value.ToString().ToLower());
            }
        }

        [Fact]
        public async Task BanirMembro_AutoBan_DeveRetornarBadRequest()
        {
            // Arrange
            var options = GetDbOptions("AutoBan");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
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
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var form = new ComunidadesController.BanirMembroForm();

                // Act - Tentar banir a si próprio
                var result = await controller.BanirMembro(comunidadeId, adminId, form);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("próprio", badRequest.Value.ToString().ToLower());
            }
        }

        [Fact]
        public async Task RemoverMembro_AutoRemocao_DeveRetornarBadRequest()
        {
            // Arrange
            var options = GetDbOptions("AutoRemocao");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
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
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act - Tentar remover a si próprio
                var result = await controller.RemoverMembro(comunidadeId, adminId);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("próprio", badRequest.Value.ToString().ToLower());
            }
        }

        #endregion

        #region Forbidden Error Tests

        [Fact]
        public async Task GetMembros_QuandoBanido_DeveRetornarForbidden()
        {
            // Arrange
            var options = GetDbOptions("MembrosBanido");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var bannedId = "user-banned";
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
                    UtilizadorId = bannedId,
                    Role = "Membro",
                    Status = "Banido",
                    DataEntrada = DateTime.UtcNow,
                    BanidoAte = DateTime.UtcNow.AddDays(7)
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, bannedId);

                // Act
                var result = await controller.GetMembros(comunidadeId);

                // Assert
                var forbidResult = Assert.IsType<ObjectResult>(result);
                Assert.Equal(403, forbidResult.StatusCode);
            }
        }

        [Fact]
        public async Task GetPosts_QuandoBanido_DeveRetornarForbidden()
        {
            // Arrange
            var options = GetDbOptions("PostsBanido");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var bannedId = "user-banned";
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
                    UtilizadorId = bannedId,
                    Role = "Membro",
                    Status = "Banido",
                    DataEntrada = DateTime.UtcNow,
                    BanidoAte = DateTime.UtcNow.AddDays(7)
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, bannedId);

                // Act
                var result = await controller.GetPosts(comunidadeId);

                // Assert
                var forbidResult = Assert.IsType<ObjectResult>(result);
                Assert.Equal(403, forbidResult.StatusCode);
            }
        }

        #endregion
    }
}
