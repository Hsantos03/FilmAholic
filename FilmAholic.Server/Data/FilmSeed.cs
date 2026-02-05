using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data
{
    public static class FilmSeed
    {
        public static List<Filme> Filmes = new()
    {
        new Filme { Id = 1, Titulo = "Zootopia", Duracao = 108, Genero = "Animação", PosterUrl = "https://image.tmdb.org/t/p/w500/hlK0e0wAQ3VLuJcsfIYPvb4JVud.jpg", Ano = 2016 },
        new Filme { Id = 2, Titulo = "Inception", Duracao = 148, Genero = "Sci-Fi", PosterUrl = "https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg", Ano = 2010 },
        new Filme { Id = 3, Titulo = "Interstellar", Duracao = 169, Genero = "Sci-Fi", PosterUrl = "https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg", Ano = 2014 }
    };
    }
}
