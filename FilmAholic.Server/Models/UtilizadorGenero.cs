namespace FilmAholic.Server.Models;

// Tabela de junção para relação many-to-many entre Utilizador e Genero
public class UtilizadorGenero
{
    public string UtilizadorId { get; set; } = "";
    public int GeneroId { get; set; }
    public DateTime DataAdicao { get; set; } = DateTime.UtcNow;

    // Navegação properties
    public Utilizador Utilizador { get; set; } = null!;
    public Genero Genero { get; set; } = null!;
}
