using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa o feedback de uma recomendação de filme por um utilizador.
/// </summary>
public class RecomendacaoFeedback
{
    public int Id { get; set; }

    [Required]
    public string UtilizadorId { get; set; } = string.Empty;

    public int FilmeId { get; set; }
    public Filme? Filme { get; set; }

    public bool Relevante { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}