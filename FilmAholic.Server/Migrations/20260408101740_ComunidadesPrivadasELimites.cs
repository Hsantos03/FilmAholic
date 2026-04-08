using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class ComunidadesPrivadasELimites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "NotificacoesComunidade",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "NotificacoesComunidade",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "post");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivada",
                table: "Comunidades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LimiteMembros",
                table: "Comunidades",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComunidadePedidosEntrada",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComunidadeId = table.Column<int>(type: "int", nullable: false),
                    UtilizadorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DataPedido = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DataResposta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondidoPorId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComunidadePedidosEntrada", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComunidadePedidosEntrada_AspNetUsers_UtilizadorId",
                        column: x => x.UtilizadorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComunidadePedidosEntrada_Comunidades_ComunidadeId",
                        column: x => x.ComunidadeId,
                        principalTable: "Comunidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePedidosEntrada_ComunidadeId_UtilizadorId_Status",
                table: "ComunidadePedidosEntrada",
                columns: new[] { "ComunidadeId", "UtilizadorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ComunidadePedidosEntrada_UtilizadorId",
                table: "ComunidadePedidosEntrada",
                column: "UtilizadorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "NotificacoesComunidade");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "NotificacoesComunidade",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropTable(
                name: "ComunidadePedidosEntrada");

            migrationBuilder.DropColumn(
                name: "IsPrivada",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "LimiteMembros",
                table: "Comunidades");
        }
    }
}
