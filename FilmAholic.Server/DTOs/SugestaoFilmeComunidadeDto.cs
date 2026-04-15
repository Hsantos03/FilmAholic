namespace FilmAholic.Server.DTOs;

/// FR68 — sugestão social: filme popular entre outros membros da comunidade.
/// <summary>
/// Representa uma sugestão de filme baseada na popularidade entre os membros da comunidade.
/// </summary>
public class SugestaoFilmeComunidadeDto
{
    public int FilmeId { get; set; }
    public string Titulo { get; set; } = "";
    public string Genero { get; set; } = "";
    public string PosterUrl { get; set; } = "";
    public int Duracao { get; set; }
    public int? Ano { get; set; }
    public DateTime? ReleaseDate { get; set; }

    public int ComunidadeId { get; set; }
    public string ComunidadeNome { get; set; } = "";
    /// Número de membros distintos (excl. o utilizador) que marcaram o filme como visto.
    public int MembrosQueViram { get; set; }
}
