using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data
{
    public static class MedalhaSeed
    {
        public static List<Medalha> Medalhas = new()
        {
            // Medalhas de Filmes Vistos
            new Medalha { Id = 1, Nome = "Explorador Cinéfilo", Descricao = "Viste 50 filmes.", IconeUrl = "/uploads/comunidades/icons/filmesVistos/50_FilmesVistos.png", CriterioQuantidade = 50, CriterioTipo = "filmesVistos" },
            new Medalha { Id = 2, Nome = "Entusiasta do Cinema", Descricao = "Viste 100 filmes.", IconeUrl = "/uploads/comunidades/icons/filmesVistos/100_FilmesVistos.png", CriterioQuantidade = 100, CriterioTipo = "filmesVistos" },
            new Medalha { Id = 3, Nome = "Mestre Cinéfilo", Descricao = "Viste 500 filmes.", IconeUrl = "/uploads/comunidades/icons/filmesVistos/500_FilmesVistos.png", CriterioQuantidade = 500, CriterioTipo = "filmesVistos" },
            new Medalha { Id = 4, Nome = "Lenda do Cinema", Descricao = "Viste 1000 filmes.", IconeUrl = "/uploads/comunidades/icons/filmesVistos/1000_FilmesVistos.png", CriterioQuantidade = 1000, CriterioTipo = "filmesVistos" },

            // Medalhas de Nível
            new Medalha { Id = 5, Nome = "Iniciante", Descricao = "Alcançaste o nível 10.", IconeUrl = "/uploads/comunidades/icons/Nivel/Nivel_10.png", CriterioQuantidade = 10, CriterioTipo = "nivel" },
            new Medalha { Id = 6, Nome = "Experiente", Descricao = "Alcançaste o nível 50.", IconeUrl = "/uploads/comunidades/icons/Nivel/Nivel_50.png", CriterioQuantidade = 50, CriterioTipo = "nivel" },
            new Medalha { Id = 7, Nome = "Mestre", Descricao = "Alcançaste o nível 100.", IconeUrl = "/uploads/comunidades/icons/Nivel/Nivel_100.png", CriterioQuantidade = 100, CriterioTipo = "nivel" },
            
            // Medalhas de Desafios Diários
            new Medalha { Id = 8, Nome = "Amador dos Desafios", Descricao = "Completaste 7 desafios diários.", IconeUrl = "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_7.png", CriterioQuantidade = 7, CriterioTipo = "desafiosDiarios" },
            new Medalha { Id = 9, Nome = "Experiente em Desafios", Descricao = "Completaste 30 desafios diários.", IconeUrl = "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_30.png", CriterioQuantidade = 30, CriterioTipo = "desafiosDiarios" },
            new Medalha { Id = 10, Nome = "Mestre dos Desafios", Descricao = "Completaste 150 desafios diários.", IconeUrl = "/uploads/comunidades/icons/DesafiosDiarios/DesafiosDiarios_150.png", CriterioQuantidade = 150, CriterioTipo = "desafiosDiarios" },

            // Medalhas de HigherOrLower
            new Medalha { Id = 11, Nome = "Iniciante da Adivinhação", Descricao = "Acertaste 5 vezes seguidas no Higher or Lower.", IconeUrl = "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_5.png", CriterioQuantidade = 5, CriterioTipo = "higherOrLower" },
            new Medalha { Id = 12, Nome = "Experiente da Adivinhação", Descricao = "Acertaste 10 vezes seguidas no Higher or Lower.", IconeUrl = "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_10.png", CriterioQuantidade = 10, CriterioTipo = "higherOrLower" },
            new Medalha { Id = 13, Nome = "Mestre da Adivinhação", Descricao = "Acertaste 25 vezes seguidas no Higher or Lower.", IconeUrl = "/uploads/comunidades/icons/HigherOrLower/HigherOrLower_25.png", CriterioQuantidade = 25, CriterioTipo = "higherOrLower" },

            // Medalhas de Comunidades
            new Medalha { Id = 14, Nome = "Fundador", Descricao = "Criaste a tua primeira comunidade.", IconeUrl = "/uploads/comunidades/icons/Comunidades/CriarComunidade.png", CriterioQuantidade = 1, CriterioTipo = "criarComunidade" },
            new Medalha { Id = 15, Nome = "Participante", Descricao = "Juntaste-te a uma comunidade.", IconeUrl = "/uploads/comunidades/icons/Comunidades/JuntarComunidade.png", CriterioQuantidade = 1, CriterioTipo = "juntarComunidade" }
        };
    }
}
