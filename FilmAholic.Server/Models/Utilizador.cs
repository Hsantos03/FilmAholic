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
    public string? GeneroFavorito { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

}