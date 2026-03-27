FilmAholic.Server\Models\ComunidadeMembro.cs
using System;

namespace FilmAholic.Server.Models;

public class ComunidadeMembro
{
    public int Id { get; set; }
    public int ComunidadeId { get; set; }
    public string UtilizadorId { get; set; } = "";
    public string Role { get; set; } = "Membro"; // e.g. Admin, Moderador, Membro
    public string Status { get; set; } = "Ativo"; // e.g. Ativo, Banido
    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;

    public Comunidade Comunidade { get; set; } = null!;
    public Utilizador Utilizador { get; set; } = null!;
}