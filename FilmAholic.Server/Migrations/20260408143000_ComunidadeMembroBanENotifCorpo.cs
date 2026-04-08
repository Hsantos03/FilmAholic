using System;
using FilmAholic.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FilmAholicDbContext))]
    [Migration("20260408143000_ComunidadeMembroBanENotifCorpo")]
    public class ComunidadeMembroBanENotifCorpo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BanidoAte",
                table: "ComunidadeMembros",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoBan",
                table: "ComunidadeMembros",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Corpo",
                table: "NotificacoesComunidade",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Corpo",
                table: "NotificacoesComunidade");

            migrationBuilder.DropColumn(
                name: "MotivoBan",
                table: "ComunidadeMembros");

            migrationBuilder.DropColumn(
                name: "BanidoAte",
                table: "ComunidadeMembros");
        }
    }
}
