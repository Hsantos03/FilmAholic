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

namespace FilmAholic.Tests.DataIntegrityTests
{
    public class ComunidadesDataIntegrityTests
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

        #region Comunidade-Admin Relationship Tests

        [Fact]
        public async Task Comunidade_CriadorDeveSerAdmin()
        {
            // Arrange
            var options = GetDbOptions("CriadorAdmin");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var creatorId = "user-creator";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var controller = CreateControllerWithUser(context, env, logger, creatorId);
                controller.ControllerContext.HttpContext.Request.Scheme = "https";
                controller.ControllerContext.HttpContext.Request.Host = new HostString("test.com");

                var form = new ComunidadesController.ComunidadeCreateForm
                {
                    Nome = "Comunidade Criada",
                    Descricao = "Descrição"
                };

                // Act
                var result = await controller.Create(form);
                var createdResult = Assert.IsType<CreatedAtActionResult>(result);
                var dto = Assert.IsType<ComunidadesController.ComunidadeDto>(createdResult.Value);
                comunidadeId = dto.Id;
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Assert
                var membro = await context.ComunidadeMembros
                    .FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == creatorId);
                Assert.NotNull(membro);
                Assert.Equal("Admin", membro.Role);
                Assert.Equal("Ativo", membro.Status);
            }
        }

        [Fact]
        public async Task Comunidade_SempreDeveTerPeloMenosUmAdmin()
        {
            // Arrange
            var options = GetDbOptions("MinimoUmAdmin");
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
                // Act & Assert - Admin pode ver membros
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var result = await controller.GetMembros(comunidadeId);
                var okResult = Assert.IsType<OkObjectResult>(result);
                var membros = Assert.IsType<List<ComunidadesController.MembroDto>>(okResult.Value);
                Assert.Contains(membros, m => m.Role == "Admin");
            }
        }

        #endregion

        #region Post-Comunidade Relationship Tests

        [Fact]
        public async Task Post_DevePertencerAUmaComunidade()
        {
            // Arrange
            var options = GetDbOptions("PostComunidade");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-post";
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

                context.ComunidadePosts.Add(new ComunidadePost
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = userId,
                    Titulo = "Post Teste",
                    Conteudo = "Conteúdo",
                    DataCriacao = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act
                var post = await context.ComunidadePosts
                    .Include(p => p.Comunidade)
                    .FirstAsync(p => p.ComunidadeId == comunidadeId);

                // Assert
                Assert.NotNull(post.Comunidade);
                Assert.Equal("Comunidade", post.Comunidade.Nome);
            }
        }

        #endregion

        #region Membro-Comunidade Status Tests

        [Fact]
        public async Task Membro_BanidoNaoDeveContarParaMembrosAtivos()
        {
            // Arrange
            var options = GetDbOptions("BanidoNaoConta");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var adminId = "user-admin";
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

                // Admin
                context.ComunidadeMembros.Add(new ComunidadeMembro
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = adminId,
                    Role = "Admin",
                    Status = "Ativo",
                    DataEntrada = DateTime.UtcNow
                });

                // Banido
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
                // Act
                var membrosAtivos = await context.ComunidadeMembros
                    .CountAsync(m => m.ComunidadeId == comunidadeId && m.Status == "Ativo");

                // Assert
                Assert.Equal(1, membrosAtivos);
            }
        }

        #endregion

        #region PedidoEntrada Integrity Tests

        [Fact]
        public async Task PedidoEntrada_SomenteUmPedidoPendentePorUtilizador()
        {
            // Arrange
            var options = GetDbOptions("PedidoUnico");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-pedido";
            int comunidadeId;

            using (var context = new FilmAholicDbContext(options))
            {
                var comunidade = new Comunidade
                {
                    Nome = "Comunidade Privada",
                    IsPrivada = true,
                    DataCriacao = DateTime.UtcNow
                };
                context.Comunidades.Add(comunidade);
                await context.SaveChangesAsync();
                comunidadeId = comunidade.Id;

                // Criar primeiro pedido
                context.ComunidadePedidosEntrada.Add(new ComunidadePedidoEntrada
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = userId,
                    Status = "Pendente",
                    DataPedido = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act - Tentar criar segundo pedido
                var controller = CreateControllerWithUser(context, env, logger, userId);
                var result = await controller.Juntar(comunidadeId);

                // Assert - Deve ser rejeitado
                var conflictResult = Assert.IsType<ConflictObjectResult>(result);
                Assert.Contains("pedido pendente", conflictResult.Value.ToString().ToLower());
            }
        }

        [Fact]
        public async Task PedidoEntrada_AprovadoDeveCriarMembro()
        {
            // Arrange
            var options = GetDbOptions("PedidoAprovado");
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
                // Act
                var controller = CreateControllerWithUser(context, env, logger, adminId);
                var result = await controller.AprovarPedidoEntrada(comunidadeId, pedidoId);

                // Assert
                var okResult = Assert.IsType<OkObjectResult>(result);
                var membro = await context.ComunidadeMembros
                    .FirstOrDefaultAsync(m => m.ComunidadeId == comunidadeId && m.UtilizadorId == requesterId);
                Assert.NotNull(membro);
                Assert.Equal("Ativo", membro.Status);

                var pedido = await context.ComunidadePedidosEntrada.FindAsync(pedidoId);
                Assert.Equal("Aprovado", pedido.Status);
            }
        }

        #endregion

        #region Voto Integrity Tests

        [Fact]
        public async Task Voto_MembroSoPodeVotarUmaVezPorPost()
        {
            // Arrange
            var options = GetDbOptions("VotoUnico");
            var env = CreateMockWebHostEnvironment();
            var logger = CreateMockLogger();
            var userId = "user-voto";
            int comunidadeId;
            int postId;

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

                var post = new ComunidadePost
                {
                    ComunidadeId = comunidadeId,
                    UtilizadorId = "other-user",
                    Titulo = "Post",
                    Conteudo = "Conteúdo",
                    DataCriacao = DateTime.UtcNow
                };
                context.ComunidadePosts.Add(post);
                await context.SaveChangesAsync();
                postId = post.Id;

                // Primeiro voto
                context.ComunidadePostVotos.Add(new ComunidadePostVoto
                {
                    PostId = postId,
                    UtilizadorId = userId,
                    IsLike = true
                });
                await context.SaveChangesAsync();
            }

            using (var context = new FilmAholicDbContext(options))
            {
                // Act - Contar votos do utilizador no post
                var votosCount = await context.ComunidadePostVotos
                    .CountAsync(v => v.PostId == postId && v.UtilizadorId == userId);

                // Assert
                Assert.Equal(1, votosCount);
            }
        }

        #endregion
    }
}
