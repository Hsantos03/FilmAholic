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
using Moq;
using Xunit;

namespace FilmAholic.Tests.UnitTests
{
    public class ComunidadesUnitTests
    {
        private static DbContextOptions<FilmAholicDbContext> GetDbOptions(string dbName)
        {
            return new DbContextOptionsBuilder<FilmAholicDbContext>()
                .UseInMemoryDatabase(databaseName: dbName + "_" + Guid.NewGuid())
                .Options;
        }

        private static ComunidadesController CreateControllerWithUser(FilmAholicDbContext context, IWebHostEnvironment env, ILogger<ComunidadesController> logger, string userId)
        {
            var controller = new ComunidadesController(context, env, logger);
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

        #region GetAll Tests

        [Fact]
        public async Task GetAll_DeveRetornarListaDeComunidades()
        {
            // Arrange
            var options = GetDbOptions("GetAll");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();

            using (var context = new FilmAholicDbContext(options))
            {
                context.Comunidades.Add(new Comunidade
                {
                    Nome = "Comunidade Teste 1",
                    Descricao = "Descricao 1",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false
                });
                context.Comunidades.Add(new Comunidade
                {
                    Nome = "Comunidade Teste 2",
                    Descricao = "Descricao 2",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = true
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ComunidadesController(context, env, logger);
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                // Act
                var result = await controller.GetAll();

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var list = Assert.IsType<List<ComunidadesController.ComunidadeDto>>(okResult.Value);
                Assert.Equal(2, list.Count);
            }
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_DeveRetornarComunidade_QuandoExiste()
        {
            // Arrange
            var options = GetDbOptions("GetById_Existe");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Teste",
                    Descricao = "Descricao Teste",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ComunidadesController(context, env, logger);
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                };

                // Act
                var result = await controller.GetById(comunidadeId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var dto = Assert.IsType<ComunidadesController.ComunidadeDto>(okResult.Value);
                Assert.Equal("Comunidade Teste", dto.Nome);
                Assert.Equal("Descricao Teste", dto.Descricao);
            }
        }

        [Fact]
        public async Task GetById_DeveRetornarNotFound_QuandoNaoExiste()
        {
            // Arrange
            var options = GetDbOptions("GetById_NaoExiste");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = new ComunidadesController(context, env, logger);

                // Act
                var result = await controller.GetById(999);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        #endregion

        #region GetMembros Tests

        [Fact]
        public async Task GetMembros_DeveRetornarMembrosAtivos()
        {
            // Arrange
            var options = GetDbOptions("GetMembros");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-test-123";
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

                // Adicionar utilizador
                context.Users.Add(new Utilizador
                {
                    Id = userId,
                    UserName = "testuser",
                    Email = "test@test.com",
                    Nome = "Test",
                    Sobrenome = "User"
                });

                // Membro ativo
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = userId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                // Membro banido (não deve aparecer)
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = "user-banido",
                    Role = "Membro",
                    Status = "Banido",
                    DataEntrada = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.GetMembros(comunidadeId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var membros = Assert.IsType<List<ComunidadesController.MembroDto>>(okResult.Value);
                Assert.Single(membros);
                Assert.Equal("Admin", membros[0].Role);
            }
        }

        #endregion

        #region Juntar Tests

        [Fact]
        public async Task Juntar_DeveAdicionarMembro_QuandoComunidadePublica()
        {
            // Arrange
            var options = GetDbOptions("Juntar_Publica");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-novo";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Publica",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = false
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.Juntar(comunidadeId);

                // Assert
                Assert.IsType<OkResult>(result);
                var membro = await context.ComunidadeMembros.FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == userId);
                Assert.NotNull(membro);
                Assert.Equal("Membro", membro.Role);
                Assert.Equal("Ativo", membro.Status);
            }
        }

        [Fact]
        public async Task Juntar_DeveRetornarConflict_QuandoJaEhMembro()
        {
            // Arrange
            var options = GetDbOptions("Juntar_Conflict");
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
                Assert.Contains("Já és membro", conflictResult.Value.ToString());
            }
        }

        [Fact]
        public async Task Juntar_DeveCriarPedido_QuandoComunidadePrivada()
        {
            // Arrange
            var options = GetDbOptions("Juntar_Privada");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-pedido";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Privada",
                    DataCriacao = DateTime.UtcNow,
                    IsPrivada = true
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.Juntar(comunidadeId);

                // Assert
                var acceptedResult = Assert.IsType<AcceptedResult>(result);
                Assert.Contains("pendingApproval", acceptedResult.Value.ToString());

                var pedido = await context.ComunidadePedidosEntrada.FirstOrDefaultAsync(p => p.ComunidadeId == comunidadeId && p.UtilizadorId == userId);
                Assert.NotNull(pedido);
                Assert.Equal("Pendente", pedido.Status);
            }
        }

        [Fact]
        public async Task Juntar_DeveRetornarForbidden_QuandoBanido()
        {
            // Arrange
            var options = GetDbOptions("Juntar_Banido");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-banido";
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
                    Status = "Banido",
                    DataEntrada = DateTime.UtcNow,
                    BanidoAte = DateTime.UtcNow.AddDays(7)
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.Juntar(comunidadeId);

                // Assert
                var forbidResult = Assert.IsType<ObjectResult>(result);
                Assert.Equal(403, forbidResult.StatusCode);
            }
        }

        #endregion

        #region GetMeuEstado Tests

        [Fact]
        public async Task GetMeuEstado_DeveRetornarEstadoCorreto_QuandoMembro()
        {
            // Arrange
            var options = GetDbOptions("MeuEstado_Membro");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-membro";
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
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.GetMeuEstado(comunidadeId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                var estadoJson = okResult.Value.ToString();
                Assert.Contains("isMembro", estadoJson);
                Assert.Contains("isAdmin", estadoJson);
                Assert.Contains("isBanned", estadoJson);
            }
        }

        [Fact]
        public async Task GetMeuEstado_DeveRetornarEstadoCorreto_QuandoNaoMembro()
        {
            // Arrange
            var options = GetDbOptions("MeuEstado_NaoMembro");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-nao-membro";
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
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.GetMeuEstado(comunidadeId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                var estadoJson = okResult.Value.ToString();
                Assert.Contains("isMembro", estadoJson);
                Assert.Contains("isAdmin", estadoJson);
            }
        }

        #endregion

        #region GetPedidosEntrada Tests

        [Fact]
        public async Task GetPedidosEntrada_DeveRetornarPedidosPendentes_QuandoAdmin()
        {
            // Arrange
            var options = GetDbOptions("GetPedidos");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
            var requesterId = "user-requester";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade",
                    IsPrivada = true,
                    DataCriacao = DateTime.UtcNow
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

                // Pedido pendente
                context.ComunidadePedidosEntrada.Add(new ComunidadePedidoEntrada
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = requesterId,
                    Status = "Pendente",
                    DataPedido = DateTime.UtcNow
                });

                context.Users.Add(new Utilizador
                {
                    Id = requesterId,
                    UserName = "requester",
                    Email = "req@test.com",
                    Nome = "Request",
                    Sobrenome = "User"
                });

                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, adminId);

                // Act
                var result = await controller.GetPedidosEntrada(comunidadeId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var pedidos = Assert.IsType<List<ComunidadesController.ComunidadePedidoEntradaDto>>(okResult.Value);
                Assert.Single(pedidos);
                Assert.Equal(requesterId, pedidos[0].UtilizadorId);
            }
        }

        [Fact]
        public async Task GetPedidosEntrada_DeveRetornarForbid_QuandoNaoAdmin()
        {
            // Arrange
            var options = GetDbOptions("GetPedidos_NaoAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-membro";
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
                var result = await controller.GetPedidosEntrada(comunidadeId);

                // Assert
                Assert.IsType<ForbidResult>(result);
            }
        }

        #endregion

        #region AprovarPedidoEntrada Tests

        [Fact]
        public async Task AprovarPedidoEntrada_DeveAprovarPedido()
        {
            // Arrange
            var options = GetDbOptions("AprovarPedido");
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
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                Assert.Contains("Pedido aprovado", okResult.Value.ToString());

                var pedido = await context.ComunidadePedidosEntrada.FindAsync(pedidoId);
                Assert.Equal("Aprovado", pedido.Status);

                var membro = await context.ComunidadeMembros.FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == requesterId);
                Assert.NotNull(membro);
                Assert.Equal("Ativo", membro.Status);
            }
        }

        #endregion

        #region Sair Tests

        [Fact]
        public async Task Sair_DeveRemoverMembro()
        {
            // Arrange
            var options = GetDbOptions("Sair");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-sair";
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
                var result = await controller.Sair(comunidadeId);

                // Assert
                Assert.IsType<OkResult>(result);
                var membro = await context.ComunidadeMembros.FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == userId);
                Assert.Null(membro);
            }
        }

        [Fact]
        public async Task Sair_DeveRetornarNotFound_QuandoNaoEhMembro()
        {
            // Arrange
            var options = GetDbOptions("Sair_NaoMembro");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-nao-membro";
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
                var controller = CreateControllerWithUser(context, env, logger, userId);

                // Act
                var result = await controller.Sair(comunidadeId);

                // Assert
                Assert.IsType<NotFoundResult>(result);
            }
        }

        #endregion

        #region BanirMembro Tests

        [Fact]
        public async Task BanirMembro_DeveBanirMembro()
        {
            // Arrange
            var options = GetDbOptions("Banir");
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
                    DuracaoDias = 7,
                    Motivo = "Comportamento inapropriado"
                };

                // Act
                var result = await controller.BanirMembro(comunidadeId, membroId, form);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                Assert.NotNull(okResult.Value);
                Assert.Contains("Membro banido", okResult.Value.ToString());

                var membro = await context.ComunidadeMembros.FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == membroId);
                Assert.Equal("Banido", membro.Status);
                Assert.NotNull(membro.BanidoAte);
                Assert.Equal("Comportamento inapropriado", membro.MotivoBan);
            }
        }

        [Fact]
        public async Task BanirMembro_DeveRetornarBadRequest_QuandoAutoBan()
        {
            // Arrange
            var options = GetDbOptions("Banir_Auto");
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

                // Act
                var result = await controller.BanirMembro(comunidadeId, adminId, form);

                // Assert
                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.NotNull(badRequest.Value);
                Assert.Contains("Não te podes banir", badRequest.Value.ToString());
            }
        }

        #endregion

        #region CastigarMembro Tests

        [Fact]
        public async Task CastigarMembro_DeveAplicarCastigo()
        {
            // Arrange
            var options = GetDbOptions("Castigar");
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
                var result = await controller.CastigarMembro(comunidadeId, membroId, 24);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                // Verificar apenas o conteúdo sem usar dynamic
                Assert.NotNull(okResult.Value);
                Assert.Contains("castigado", okResult.Value.ToString().ToLower());

                var membro = await context.ComunidadeMembros.FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == membroId);
                Assert.NotNull(membro);
                Assert.NotNull(membro.CastigadoAte);
                Assert.True(membro.CastigadoAte > DateTime.UtcNow.AddHours(23));
            }
        }

        #endregion
    }
}
