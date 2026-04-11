using System;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um cache de filmes em cinema.
/// </summary>
public class CinemaMovieCache
{
    public int Id { get; set; }
    public string MovieId { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Poster { get; set; } = string.Empty;
    public string Cinema { get; set; } = string.Empty;
    public string HorariosJson { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string Duracao { get; set; } = string.Empty;
    public string Classificacao { get; set; } = string.Empty;
    public string Idioma { get; set; } = string.Empty;
    public string Sala { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime DataCache { get; set; } = DateTime.UtcNow;
}
