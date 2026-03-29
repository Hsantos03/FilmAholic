using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class NotificacoesUniqueSoComFilmeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_UtilizadorId_FilmeId_Tipo",
                table: "Notificacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId_FilmeId_Tipo",
                table: "Notificacoes",
                columns: new[] { "UtilizadorId", "FilmeId", "Tipo" },
                unique: true,
                filter: "[FilmeId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notificacoes_UtilizadorId_FilmeId_Tipo",
                table: "Notificacoes");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacoes_UtilizadorId_FilmeId_Tipo",
                table: "Notificacoes",
                columns: new[] { "UtilizadorId", "FilmeId", "Tipo" },
                unique: true);
        }
    }
}
