using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa uma medalha que pode ser obtida por um utilizador.
/// </summary>
public class Medalha
{
    public int Id { get; set; }
    [Required]
    public string Nome { get; set; } = string.Empty;
    [Required]
    public string Descricao { get; set; } = string.Empty;
    [Required]
    public string IconeUrl { get; set; } = string.Empty;
    public int CriterioQuantidade { get; set; }
    [Required]
    public string CriterioTipo { get; set; } = string.Empty;
    public bool Ativa { get; set; } = true;

    // Navegação
    public ICollection<UtilizadorMedalha> UtilizadorMedalhas { get; set; } = new List<UtilizadorMedalha>();
}
