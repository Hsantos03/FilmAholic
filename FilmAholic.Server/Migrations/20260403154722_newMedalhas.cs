using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class newMedalhas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 8,
                column: "Nome",
                value: "Amador dos Desafios");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 9,
                column: "Nome",
                value: "Experiente em Desafios");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Descricao", "Nome" },
                values: new object[] { "Acertaste 5 vezes seguidas no Higher or Lower.", "Iniciante da Adivinhação" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Descricao", "Nome" },
                values: new object[] { "Acertaste 10 vezes seguidas no Higher or Lower.", "Experiente da Adivinhação" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 13,
                column: "Descricao",
                value: "Acertaste 25 vezes seguidas no Higher or Lower.");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 14,
                column: "Nome",
                value: "Fundador");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 8,
                column: "Nome",
                value: "Desafioso Iniciante");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 9,
                column: "Nome",
                value: "Desafioso Dedicado");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Descricao", "Nome" },
                values: new object[] { "Acertaste 5 vezes no Higher or Lower.", "Adivinha Iniciante" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Descricao", "Nome" },
                values: new object[] { "Acertaste 10 vezes no Higher or Lower.", "Adivinha Experiente" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 13,
                column: "Descricao",
                value: "Acertaste 25 vezes no Higher or Lower.");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 14,
                column: "Nome",
                value: "Membro Ativo");
        }
    }
}
