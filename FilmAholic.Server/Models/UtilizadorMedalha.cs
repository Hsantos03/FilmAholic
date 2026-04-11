using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa a relação entre um utilizador e uma medalha.
/// </summary>
public class UtilizadorMedalha
{
    [Required]
    public string UtilizadorId { get; set; } = string.Empty;
    public int MedalhaId { get; set; }
    public DateTime DataConquista { get; set; } = DateTime.UtcNow;

    // Navegação
    public Utilizador Utilizador { get; set; } = null!;
    public Medalha Medalha { get; set; } = null!;
}
