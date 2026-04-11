using System;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um membro de uma comunidade.
/// </summary>
public class ComunidadeMembro
{
    public int Id { get; set; }
    public int ComunidadeId { get; set; }
    public string UtilizadorId { get; set; } = "";
    public string Role { get; set; } = "Membro"; // e.g. Admin, Moderador, Membro
    public string Status { get; set; } = "Ativo"; // e.g. Ativo, Banido
    public DateTime DataEntrada { get; set; } = DateTime.UtcNow;
    public DateTime? CastigadoAte { get; set; }
    /// Quando Status == Banido: null = banimento permanente; caso contrário fim do ban (UTC).
    public DateTime? BanidoAte { get; set; }
    public string? MotivoBan { get; set; }

    public Comunidade Comunidade { get; set; } = null!;
    public Utilizador Utilizador { get; set; } = null!;
}