using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa uma notificação para um utilizador.
/// </summary>
public class Notificacao
{
    public int Id { get; set; }

    public string UtilizadorId { get; set; } = string.Empty;

    [JsonIgnore]
    public Utilizador? Utilizador { get; set; }

    public int? FilmeId { get; set; }

    public Filme? Filme { get; set; }

    // "NovaEstreia"
    public string Tipo { get; set; } = string.Empty;

    /// Texto/JSON para notificações sem filme associado (ex.: <c>ResumoEstatisticas</c>).
    public string? Corpo { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
}

