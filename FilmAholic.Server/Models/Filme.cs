namespace FilmAholic.Server.Models;

public class Filme
{
    public int Id { get; set; }
    public string TmdbId { get; set; } = "";
    public string Titulo { get; set; } = "";
    public int Duracao { get; set; } // duração em minutos
}
