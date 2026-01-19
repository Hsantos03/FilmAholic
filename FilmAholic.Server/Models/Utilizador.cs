using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

public class Utilizador : IdentityUser
{
    [Required]
    public string Nome { get; set; } = "";
    [Required]
    public string Sobrenome { get; set; } = "";
    [Required]
    public DateTime DataNascimento { get; set; }
    public string? FotoPerfilUrl { get; set; }
    public string? CapaUrl { get; set; }
    public string? GeneroFavorito { get; set; } 
    public string TopFilmes { get; set; } = "[]";
    public string TopAtores { get; set; } = "[]";
    public int XP { get; set; } = 0;
    public string? Bio { get; set; } 
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegação para géneros favoritos (relação many-to-many)
    public ICollection<UtilizadorGenero> GenerosFavoritos { get; set; } = new List<UtilizadorGenero>();
}