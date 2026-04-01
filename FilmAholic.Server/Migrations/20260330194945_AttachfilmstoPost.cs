using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class AttachfilmstoPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FilmeId",
                table: "ComunidadePosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilmePosterUrl",
                table: "ComunidadePosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FilmeTitulo",
                table: "ComunidadePosts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComunidadePostId",
                table: "ComunidadePostReports",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UtilizadorId",
                table: "ComunidadePostComentarios",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "ComunidadePostId",
                table: "ComunidadePostComentarios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostReports_ComunidadePostId",
                table: "ComunidadePostReports",
                column: "ComunidadePostId");

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePostComentarios_ComunidadePostId",
                table: "ComunidadePostComentarios",
                column: "ComunidadePostId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComunidadePostComentarios_ComunidadePosts_ComunidadePostId",
                table: "ComunidadePostComentarios",
                column: "ComunidadePostId",
                principalTable: "ComunidadePosts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ComunidadePostReports_ComunidadePosts_ComunidadePostId",
                table: "ComunidadePostReports",
                column: "ComunidadePostId",
                principalTable: "ComunidadePosts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComunidadePostComentarios_ComunidadePosts_ComunidadePostId",
                table: "ComunidadePostComentarios");

            migrationBuilder.DropForeignKey(
                name: "FK_ComunidadePostReports_ComunidadePosts_ComunidadePostId",
                table: "ComunidadePostReports");

            migrationBuilder.DropIndex(
                name: "IX_ComunidadePostReports_ComunidadePostId",
                table: "ComunidadePostReports");

            migrationBuilder.DropIndex(
                name: "IX_ComunidadePostComentarios_ComunidadePostId",
                table: "ComunidadePostComentarios");

            migrationBuilder.DropColumn(
                name: "FilmeId",
                table: "ComunidadePosts");

            migrationBuilder.DropColumn(
                name: "FilmePosterUrl",
                table: "ComunidadePosts");

            migrationBuilder.DropColumn(
                name: "FilmeTitulo",
                table: "ComunidadePosts");

            migrationBuilder.DropColumn(
                name: "ComunidadePostId",
                table: "ComunidadePostReports");

            migrationBuilder.DropColumn(
                name: "ComunidadePostId",
                table: "ComunidadePostComentarios");

            migrationBuilder.AlterColumn<string>(
                name: "UtilizadorId",
                table: "ComunidadePostComentarios",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
