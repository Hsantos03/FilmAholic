using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um género de filme.
/// </summary>
public class Genero
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = "";
    
    // Navegação para utilizadores que têm este género como favorito
    public ICollection<UtilizadorGenero> Utilizadores { get; set; } = new List<UtilizadorGenero>();
}
