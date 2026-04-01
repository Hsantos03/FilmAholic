using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddSpoilerToPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TemSpoiler",
                table: "ComunidadePosts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemSpoiler",
                table: "ComunidadePosts");
        }
    }
}
