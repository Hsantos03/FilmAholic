using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddQuizPropertiesToDesafios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Acertou",
                table: "UserDesafios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Respondido",
                table: "UserDesafios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OpcaoA",
                table: "Desafios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpcaoB",
                table: "Desafios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OpcaoC",
                table: "Desafios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Pergunta",
                table: "Desafios",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RespostaCorreta",
                table: "Desafios",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Acertou",
                table: "UserDesafios");

            migrationBuilder.DropColumn(
                name: "Respondido",
                table: "UserDesafios");

            migrationBuilder.DropColumn(
                name: "OpcaoA",
                table: "Desafios");

            migrationBuilder.DropColumn(
                name: "OpcaoB",
                table: "Desafios");

            migrationBuilder.DropColumn(
                name: "OpcaoC",
                table: "Desafios");

            migrationBuilder.DropColumn(
                name: "Pergunta",
                table: "Desafios");

            migrationBuilder.DropColumn(
                name: "RespostaCorreta",
                table: "Desafios");
        }
    }
}
