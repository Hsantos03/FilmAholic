using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class ReminderHigherOrLower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ReminderJogoAtiva",
                table: "PreferenciasNotificacao",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReminderJogoAtiva",
                table: "PreferenciasNotificacao");
        }
    }
}
