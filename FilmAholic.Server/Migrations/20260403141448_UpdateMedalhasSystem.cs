using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FilmAholic.Server.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMedalhasSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Favorito",
                table: "UserMovies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 50, "filmesVistos", "Viste 50 filmes.", "/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png", "Explorador Cinéfilo" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 100, "filmesVistos", "Viste 100 filmes.", "/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png", "Entusiasta do Cinema" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 500, "filmesVistos", "Viste 500 filmes.", "/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png", "Mestre Cinéfilo" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 1000, "filmesVistos", "Viste 1000 filmes.", "/uploads/comunidades/icons/filmesVistos/1000_FilmesVistos.png", "Lenda do Cinema" });

            migrationBuilder.InsertData(
                table: "Medalhas",
                columns: new[] { "Id", "Ativa", "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[,]
                {
                    { 5, true, 10, "nivel", "Alcançaste o nível 10.", "/uploads/comunidades/icons/Nivel/Nivel_10.png", "Iniciante" },
                    { 6, true, 50, "nivel", "Alcançaste o nível 50.", "/uploads/comunidades/icons/Nivel/Nivel_50.png", "Experiente" },
                    { 7, true, 100, "nivel", "Alcançaste o nível 100.", "/uploads/comunidades/icons/Nivel/Nivel_100.png", "Mestre" },
                    { 8, true, 7, "desafiosDiarios", "Completaste 7 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_7.png", "Desafioso Iniciante" },
                    { 9, true, 30, "desafiosDiarios", "Completaste 30 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_30.png", "Desafioso Dedicado" },
                    { 10, true, 150, "desafiosDiarios", "Completaste 150 desafios diários.", "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_150.png", "Mestre dos Desafios" },
                    { 11, true, 5, "higherOrLower", "Acertaste 5 vezes no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_5.png", "Adivinha Iniciante" },
                    { 12, true, 10, "higherOrLower", "Acertaste 10 vezes no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_10.png", "Adivinha Experiente" },
                    { 13, true, 25, "higherOrLower", "Acertaste 25 vezes no Higher or Lower.", "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_25.png", "Mestre da Adivinhação" },
                    { 14, true, 1, "criarComunidade", "Criaste a tua primeira comunidade.", "/uploads/comunidades/icons/Comunidades/CriarComunidade.png", "Membro Ativo" },
                    { 15, true, 1, "juntarComunidade", "Juntaste-te a uma comunidade.", "/uploads/comunidades/icons/Comunidades/JuntarComunidade.png", "Participante" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DropColumn(
                name: "Favorito",
                table: "UserMovies");

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 5, "avaliacoes", "Avaliaste 5 filmes.", "/icons/medalhas/iniciante.svg", "Cinéfilo Iniciante" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 25, "avaliacoes", "Avaliaste 25 filmes.", "/icons/medalhas/dedicado.svg", "Cinéfilo Dedicado" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 50, "avaliacoes", "Avaliaste 50 filmes.", "/icons/medalhas/critico.svg", "Crítico de Cinema" });

            migrationBuilder.UpdateData(
                table: "Medalhas",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CriterioQuantidade", "CriterioTipo", "Descricao", "IconeUrl", "Nome" },
                values: new object[] { 10, "comentarios", "Fizeste 10 comentários.", "/icons/medalhas/comentador.svg", "Comentador" });
        }
    }
}
