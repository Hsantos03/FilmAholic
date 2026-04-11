using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa a exposição de uma medalha de um utilizador.
/// </summary>
public class UtilizadorMedalhaExposicao
{
    [Required]
    public string UtilizadorId { get; set; } = string.Empty;
    
    public int SlotIndex { get; set; } // 0, 1, or 2 for the 3 slots
    
    public int? MedalhaId { get; set; } // null if slot is empty
    
    [MaxLength(100)]
    public string? Tag { get; set; } // e.g., "fundador" - custom tag for the medal
    
    public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;

    // Navegação
    public Utilizador Utilizador { get; set; } = null!;
    public Medalha? Medalha { get; set; }
}
