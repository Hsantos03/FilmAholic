using System;

namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um pedido de entrada em uma comunidade.
/// </summary>
public class ComunidadePedidoEntrada
{
    public int Id { get; set; }
    public int ComunidadeId { get; set; }
    public string UtilizadorId { get; set; } = "";
    public string Status { get; set; } = "Pendente"; // Pendente, Aprovado, Rejeitado
    public DateTime DataPedido { get; set; } = DateTime.UtcNow;
    public DateTime? DataResposta { get; set; }
    public string? RespondidoPorId { get; set; }

    public Comunidade Comunidade { get; set; } = null!;
    public Utilizador Utilizador { get; set; } = null!;
}
