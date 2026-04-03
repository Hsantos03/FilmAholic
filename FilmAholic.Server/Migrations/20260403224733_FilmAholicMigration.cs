using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class FilmAholicMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sobrenome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataNascimento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FotoPerfilUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapaUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneroFavorito = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopFilmes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopAtores = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    XP = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    XPDiario = table.Column<int>(type: "int", nullable: false),
                    CinemasFavoritos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UltimoResetDiario = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CinemaMovieCache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MovieId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Poster = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Cinema = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HorariosJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Genero = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Duracao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Classificacao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Idioma = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Sala = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DataCache = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CinemaMovieCache", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comunidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BannerFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IconFileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comunidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Desafios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFim = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    Genero = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    QuantidadeNecessaria = table.Column<int>(type: "int", nullable: false),
                    Xp = table.Column<int>(type: "int", nullable: false),
                    Pergunta = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OpcaoA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OpcaoB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OpcaoC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RespostaCorreta = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Desafios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Filmes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TmdbId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genero = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PosterUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duracao = table.Column<int>(type: "int", nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filmes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    RoundsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Generos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medalhas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IconeUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CriterioQuantidade = table.Column<int>(type: "int", nullable: false),
                    CriterioTipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ativa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medalhas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NovaEstreiaAtiva = table.Column<bool>(type: "bit", nullable: false),
                    NovaEstreiaFrequencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResumoEstatisticasAtiva = table.Column<bool>(type: "bit", nullable: false),
                    ResumoEstatisticasFrequencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReminderJogoAtiva = table.Column<bool>(type: "bit", nullable: false),
                    FilmeDisponivelAtiva = table.Column<bool>(type: "bit", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreferenciasNotificacao_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunidadeMembros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComunidadeId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataEntrada = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CastigadoAte = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadeMembros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadeMembros_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunidadeMembros_Comunidades_ComunidadeId",
                        column: x => x.ComunidadeId,
                        principalTable: "Comunidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunidadePosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComunidadeId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ImagemUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    TemSpoiler = table.Column<bool>(type: "bit", nullable: false),
                    FilmeId = table.Column<int>(type: "int", nullable: true),
                    FilmeTitulo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FilmePosterUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadePosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadePosts_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ComunidadePosts_Comunidades_ComunidadeId",
                        column: x => x.ComunidadeId,
                        principalTable: "Comunidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDesafios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DesafioId = table.Column<int>(type: "int", nullable: false),
                    QuantidadeProgresso = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Respondido = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Acertou = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDesafios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDesafios_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDesafios_Desafios_DesafioId",
                        column: x => x.DesafioId,
                        principalTable: "Desafios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilmeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_Filmes_FilmeId",
                        column: x => x.FilmeId,
                        principalTable: "Filmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilmeId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovieRatings_Filmes_FilmeId",
                        column: x => x.FilmeId,
                        principalTable: "Filmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilmeId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Corpo = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LidaEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificacoes_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificacoes_Filmes_FilmeId",
                        column: x => x.FilmeId,
                        principalTable: "Filmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecomendacaoFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilmeId = table.Column<int>(type: "int", nullable: false),
                    Relevante = table.Column<bool>(type: "bit", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecomendacaoFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecomendacaoFeedbacks_Filmes_FilmeId",
                        column: x => x.FilmeId,
                        principalTable: "Filmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMovies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FilmeId = table.Column<int>(type: "int", nullable: false),
                    JaViu = table.Column<bool>(type: "bit", nullable: false),
                    Favorito = table.Column<bool>(type: "bit", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMovies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMovies_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserMovies_Filmes_FilmeId",
                        column: x => x.FilmeId,
                        principalTable: "Filmes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilizadorGeneros",
                columns: table => new
                {
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GeneroId = table.Column<int>(type: "int", nullable: false),
                    DataAdicao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilizadorGeneros", x => new { x.UtilizadorId, x.GeneroId });
                    table.ForeignKey(
                        name: "FK_UtilizadorGeneros_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtilizadorGeneros_Generos_GeneroId",
                        column: x => x.GeneroId,
                        principalTable: "Generos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UtilizadorMedalhas",
                columns: table => new
                {
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    MedalhaId = table.Column<int>(type: "int", nullable: false),
                    DataConquista = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UtilizadorMedalhas", x => new { x.UtilizadorId, x.MedalhaId });
                    table.ForeignKey(
                        name: "FK_UtilizadorMedalhas_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UtilizadorMedalhas_Medalhas_MedalhaId",
                        column: x => x.MedalhaId,
                        principalTable: "Medalhas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunidadePostComentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Conteudo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ComunidadePostId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadePostComentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadePostComentarios_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComunidadePostComentarios_ComunidadePosts_ComunidadePostId",
                        column: x => x.ComunidadePostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComunidadePostComentarios_ComunidadePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunidadePostReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataReport = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComunidadePostId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadePostReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadePostReports_ComunidadePosts_ComunidadePostId",
                        column: x => x.ComunidadePostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ComunidadePostReports_ComunidadePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComunidadePostVotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsLike = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadePostVotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadePostVotos_ComunidadePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificacoesComunidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ComunidadeId = table.Column<int>(type: "int", nullable: false),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LidaEm = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificacoesComunidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificacoesComunidade_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacoesComunidade_ComunidadePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotificacoesComunidade_Comunidades_ComunidadeId",
                        column: x => x.ComunidadeId,
                        principalTable: "Comunidades",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommentVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsLike = table.Column<bool>(type: "bit", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommentVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommentVotes_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Medalhas",
                columns: new[] { "Id", "Ativa", "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[,]
                {
                    { 1, true, 50, "filmesVistos", "Viste 50 filmes.", "/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png", "Explorador Cinéfilo" },
                    { 2, true, 100, "filmesVistos", "Viste 100 filmes.", "/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png", "Entusiasta do Cinema" },
                    { 3, true, 500, "filmesVistos", "Viste 500 filmes.", "/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png", "Mestre Cinéfilo" },
                    { 4, true, 1000, "filmesVistos", "Viste 1000 filmes.", "/uploads/comunidades/icons/filmesVistos/1000_FilmesVistos.png", "Lenda do Cinema" },
                    { 5, true, 10, "nivel", "Alcançaste o nível 10.", "/uploads/comunidades/icons/Nivel/Nivel_10.png", "Iniciante" },
                    { 6, true, 50, "nivel", "Alcançaste o nível 50.", "/uploads/comunidades/icons/Nivel/Nivel_50.png", "Experiente" },
                    { 7, true, 100, "nivel", "Alcançaste o nível 100.", "/uploads/comunidades/icons/Nivel/Nivel_100.png", "Mestre" },
                    { 8, true, 7, "desafiosDiarios", "Completaste 7 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_7.png", "Amador dos Desafios" },
                    { 9, true, 30, "desafiosDiarios", "Completaste 30 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_30.png", "Experiente em Desafios" },
                    { 10, true, 150, "desafiosDiarios", "Completaste 150 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_150.png", "Mestre dos Desafios" },
                    { 11, true, 5, "higherOrLower", "Acertaste 5 vezes seguidas no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_5.png", "Iniciante da Adivinhação" },
                    { 12, true, 10, "higherOrLower", "Acertaste 10 vezes seguidas no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_10.png", "Experiente da Adivinhação" },
                    { 13, true, 25, "higherOrLower", "Acertaste 25 vezes seguidas no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_25.png", "Mestre da Adivinhação" },
                    { 14, true, 1, "criarComunidade", "Criaste a tua primeira comunidade.", "/uploads/comunidades/icons/Comunidades/CriarComunidade.png", "Fundador" },
                    { 15, true, 1, "juntarComunidade", "Juntaste-te a uma comunidade.", "/uploads/comunidades/icons/Comunidades/JuntarComunidade.png", "Participante" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CinemaMovieCache_DataCache",
                table: "CinemaMovieCache",
                column: "DataCache");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_FilmeId",
                table: "Comments",
                column: "FilmeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommentVotes_CommentId_UserId",
                table: "CommentVotes",
                columns: new[] { "CommentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadeMembros_ComunidadeId",
                table: "ComunidadeMembros",
                column: "ComunidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadeMembros_UtilizadorId_ComunidadeId",
                table: "ComunidadeMembros",
                columns: new[] { "UtilizadorId", "ComunidadeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_ComunidadePostId",
                table: "ComunidadePostComentarios",
                column: "ComunidadePostId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_PostId",
                table: "ComunidadePostComentarios",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_UtilizadorId",
                table: "ComunidadePostComentarios",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostReports_ComunidadePostId",
                table: "ComunidadePostReports",
                column: "ComunidadePostId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostReports_PostId_UtilizadorId",
                table: "ComunidadePostReports",
                columns: new[] { "PostId", "UtilizadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePosts_ComunidadeId",
                table: "ComunidadePosts",
                column: "ComunidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePosts_UtilizadorId",
                table: "ComunidadePosts",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostVotos_PostId_UtilizadorId",
                table: "ComunidadePostVotos",
                columns: new[] { "PostId", "UtilizadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comunidades_Nome",
                table: "Comunidades",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Desafios_Ativo",
                table: "Desafios",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_GameHistories_UtilizadorId",
                table: "GameHistories",
                column: "UtilizadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Medalhas_Ativa",
                table: "Medalhas",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatings_FilmeId_UserId",
                table: "MovieRatings",
                columns: new[] { "FilmeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_FilmeId",
                table: "Notificacoes",
                column: "FilmeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId_FilmeId_Tipo",
                table: "Notificacoes",
                columns: new[] { "UtilizadorId", "FilmeId", "Tipo" },
                unique: true,
                filter: "[FilmeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesComunidade_ComunidadeId",
                table: "NotificacoesComunidade",
                column: "ComunidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesComunidade_PostId",
                table: "NotificacoesComunidade",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificacoesComunidade_UtilizadorId_LidaEm",
                table: "NotificacoesComunidade",
                columns: new[] { "UtilizadorId", "LidaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacao_UtilizadorId",
                table: "PreferenciasNotificacao",
                column: "UtilizadorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecomendacaoFeedbacks_FilmeId",
                table: "RecomendacaoFeedbacks",
                column: "FilmeId");

            migrationBuilder.CreateIndex(
                name: "IX_RecomendacaoFeedbacks_UtilizadorId_FilmeId",
                table: "RecomendacaoFeedbacks",
                columns: new[] { "UtilizadorId", "FilmeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDesafios_DesafioId",
                table: "UserDesafios",
                column: "DesafioId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDesafios_UtilizadorId_DesafioId",
                table: "UserDesafios",
                columns: new[] { "UtilizadorId", "DesafioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserMovies_FilmeId",
                table: "UserMovies",
                column: "FilmeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserMovies_UtilizadorId_FilmeId",
                table: "UserMovies",
                columns: new[] { "UtilizadorId", "FilmeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UtilizadorGeneros_GeneroId",
                table: "UtilizadorGeneros",
                column: "GeneroId");

            migrationBuilder.CreateIndex(
                name: "IX_UtilizadorMedalhas_MedalhaId",
                table: "UtilizadorMedalhas",
                column: "MedalhaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CinemaMovieCache");

            migrationBuilder.DropTable(
                name: "CommentVotes");

            migrationBuilder.DropTable(
                name: "ComunidadeMembros");

            migrationBuilder.DropTable(
                name: "ComunidadePostComentarios");

            migrationBuilder.DropTable(
                name: "ComunidadePostReports");

            migrationBuilder.DropTable(
                name: "ComunidadePostVotos");

            migrationBuilder.DropTable(
                name: "GameHistories");

            migrationBuilder.DropTable(
                name: "MovieRatings");

            migrationBuilder.DropTable(
                name: "Notificacoes");

            migrationBuilder.DropTable(
                name: "NotificacoesComunidade");

            migrationBuilder.DropTable(
                name: "PreferenciasNotificacao");

            migrationBuilder.DropTable(
                name: "RecomendacaoFeedbacks");

            migrationBuilder.DropTable(
                name: "UserDesafios");

            migrationBuilder.DropTable(
                name: "UserMovies");

            migrationBuilder.DropTable(
                name: "UtilizadorGeneros");

            migrationBuilder.DropTable(
                name: "UtilizadorMedalhas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ComunidadePosts");

            migrationBuilder.DropTable(
                name: "Desafios");

            migrationBuilder.DropTable(
                name: "Generos");

            migrationBuilder.DropTable(
                name: "Medalhas");

            migrationBuilder.DropTable(
                name: "Filmes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Comunidades");
        }
    }
}
