using System;

namespace FilmAholic.Server.Models;

public class NotificacaoComunidade
{
    public int Id { get; set; }

    /// User who receives the notification.
    public string UtilizadorId { get; set; } = string.Empty;
    public Utilizador? Utilizador { get; set; }

    /// Community where the new post was created.
    public int ComunidadeId { get; set; }
    public Comunidade? Comunidade { get; set; }

    /// Post that triggered the notification (optional, for direct linking).
    public int PostId { get; set; }
    public ComunidadePost? Post { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
}