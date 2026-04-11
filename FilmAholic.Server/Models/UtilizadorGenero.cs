namespace FilmAholic.Server.Models;

/// <summary>
/// Representa a relação entre um utilizador e um género.
/// </summary>
public class UtilizadorGenero
{
    public string UtilizadorId { get; set; } = "";
    public int GeneroId { get; set; }
    public DateTime DataAdicao { get; set; } = DateTime.UtcNow;

    // Navegação properties
    public Utilizador Utilizador { get; set; } = null!;
    public Genero Genero { get; set; } = null!;
}
