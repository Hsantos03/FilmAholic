namespace FilmAholic.Server.DTOs;

public class CinemaMovieDto
{
    public string Id { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Poster { get; set; } = "";
    public string Cinema { get; set; } = "";
    public List<string> Horarios { get; set; } = new();
    public string Genero { get; set; } = "";
    public string Duracao { get; set; } = "";
    public string Classificacao { get; set; } = "";
    public string Idioma { get; set; } = "";
    public string Sala { get; set; } = "";
    public string Link { get; set; } = "";
}
