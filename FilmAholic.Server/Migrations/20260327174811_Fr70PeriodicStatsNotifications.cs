using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class Fr70PeriodicStatsNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ResumoEstatisticasAtiva",
                table: "PreferenciasNotificacao",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumoEstatisticasFrequencia",
                table: "PreferenciasNotificacao",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Semanal");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "FilmeId",
                table: "Notificacoes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Corpo",
                table: "Notificacoes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResumoEstatisticasAtiva",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "ResumoEstatisticasFrequencia",
                table: "PreferenciasNotificacao");

            migrationBuilder.DropColumn(
                name: "Corpo",
                table: "Notificacoes");

            migrationBuilder.AlterColumn<string>(
                name: "Tipo",
                table: "Notificacoes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<int>(
                name: "FilmeId",
                table: "Notificacoes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
