using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMedalhaSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.InsertData(
                table: "Medalhas",
                columns: new[] { "Id", "Ativa", "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[,]
                {
                    { 1, true, 5, "avaliacoes", "Avaliaste 5 filmes.", "/icons/medalhas/iniciante.svg", "Cinéfilo Iniciante" },
                    { 2, true, 25, "avaliacoes", "Avaliaste 25 filmes.", "/icons/medalhas/dedicado.svg", "Cinéfilo Dedicado" },
                    { 3, true, 50, "avaliacoes", "Avaliaste 50 filmes.", "/icons/medalhas/critico.svg", "Crítico de Cinema" },
                    { 4, true, 10, "comentarios", "Fizeste 10 comentários.", "/icons/medalhas/comentador.svg", "Comentador" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medalhas_Ativa",
                table: "Medalhas",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_UtilizadorMedalhas_MedalhaId",
                table: "UtilizadorMedalhas",
                column: "MedalhaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UtilizadorMedalhas");

            migrationBuilder.DropTable(
                name: "Medalhas");
        }
    }
}
