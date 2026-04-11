using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class NotificacaoComunidadeComunidadeIdOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificacoesComunidade_Comunidades_ComunidadeId",
                table: "NotificacoesComunidade");

            migrationBuilder.AlterColumn<int>(
                name: "ComunidadeId",
                table: "NotificacoesComunidade",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificacoesComunidade_Comunidades_ComunidadeId",
                table: "NotificacoesComunidade",
                column: "ComunidadeId",
                principalTable: "Comunidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificacoesComunidade_Comunidades_ComunidadeId",
                table: "NotificacoesComunidade");

            migrationBuilder.AlterColumn<int>(
                name: "ComunidadeId",
                table: "NotificacoesComunidade",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificacoesComunidade_Comunidades_ComunidadeId",
                table: "NotificacoesComunidade",
                column: "ComunidadeId",
                principalTable: "Comunidades",
                principalColumn: "Id");
        }
    }
}
