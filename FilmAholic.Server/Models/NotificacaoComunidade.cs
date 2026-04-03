using System;

namespace FilmAholic.Server.Models;

public class NotificacaoComunidade
{
    public int Id { get; set; }

    /// <summary>User who receives the notification.</summary>
    public string UtilizadorId { get; set; } = string.Empty;
    public Utilizador? Utilizador { get; set; }

    /// <summary>Community where the new post was created.</summary>
    public int ComunidadeId { get; set; }
    public Comunidade? Comunidade { get; set; }

    /// <summary>Post that triggered the notification (optional, for direct linking).</summary>
    public int PostId { get; set; }
    public ComunidadePost? Post { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
}