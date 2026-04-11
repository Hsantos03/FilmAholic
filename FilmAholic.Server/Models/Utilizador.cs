using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um utilizador na aplicação.
/// </summary>
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
    public int Nivel { get; set; } = 1;
    public int XPDiario { get; set; } = 0;
    public string CinemasFavoritos { get; set; } = "[]";
    public DateTime? UltimoResetDiario { get; set; } = null;
    public string? Bio { get; set; } 
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? UserTag { get; set; } // Selected medal name as tag (e.g., "fundador")

    [MaxLength(7)]
    public string? UserTagPrimaryColor { get; set; } // Primary color hex code (e.g., "#FF4081")

    [MaxLength(7)]
    public string? UserTagSecondaryColor { get; set; } // Secondary color hex code (e.g., "#F5F5F5")

    public ICollection<UtilizadorGenero> GenerosFavoritos { get; set; } = new List<UtilizadorGenero>();

    public ICollection<UtilizadorMedalha> UtilizadorMedalhas { get; set; } = new List<UtilizadorMedalha>();
}