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
    public string? GeneroFavorito { get; set; } // Mantido para compatibilidade, pode ser removido no futuro
    public string TopFilmes { get; set; } = "[]";
    public string TopAtores { get; set; } = "[]";
    public string? Bio { get; set; } // NEW: store user's bio/profile description
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    // Navegação para géneros favoritos (relação many-to-many)
    public ICollection<UtilizadorGenero> GenerosFavoritos { get; set; } = new List<UtilizadorGenero>();
}