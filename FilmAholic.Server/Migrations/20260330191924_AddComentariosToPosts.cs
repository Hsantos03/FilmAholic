using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddComentariosToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComunidadePostComentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Conteudo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
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
                        name: "FK_ComunidadePostComentarios_ComunidadePosts_PostId",
                        column: x => x.PostId,
                        principalTable: "ComunidadePosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_PostId",
                table: "ComunidadePostComentarios",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_UtilizadorId",
                table: "ComunidadePostComentarios",
                column: "UtilizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComunidadePostComentarios");
        }
    }
}
