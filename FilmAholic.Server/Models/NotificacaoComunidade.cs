using System;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa uma notificação para a comunidade.
/// </summary>
public class NotificacaoComunidade
{
    public int Id { get; set; }

    /// User who receives the notification.
    public string UtilizadorId { get; set; } = string.Empty;
    public Utilizador? Utilizador { get; set; }

    /// Community where the new post was created. Null apÃ³s a comunidade ser apagada (ex.: notif. <c>comunidade_eliminada</c>).
    public int? ComunidadeId { get; set; }
    public Comunidade? Comunidade { get; set; }

    /// Optional post that triggered the notification.
    public int? PostId { get; set; }
    public ComunidadePost? Post { get; set; }

    /// post, pedido_entrada, etc.
    public string Tipo { get; set; } = "post";

    /// Texto extra (ex.: motivo de kick/ban) para mostrar ao utilizador.
    public string? Corpo { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? LidaEm { get; set; }
}