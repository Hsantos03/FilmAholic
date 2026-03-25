using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models;

public class Notificacao
{
    public int Id { get; set; }

    public string UtilizadorId { get; set; } = string.Empty;

    [JsonIgnore]
    public Utilizador? Utilizador { get; set; }

    public int FilmeId { get; set; }

    public Filme? Filme { get; set; }

    // e.g. "NovaEstreia"
    public string Tipo { get; set; } = string.Empty;

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
}

