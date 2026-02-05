namespace FilmAholic.Server.Models;

public class Filme
{
    public int Id { get; set; }
    public string TmdbId { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Genero { get; set; } = "";
    public string PosterUrl { get; set; } = "";
    public int Duracao { get; set; } // duração em minutos
    public int? Ano { get; set; } // ano de estreia
}
