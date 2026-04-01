using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

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