namespace FilmAholic.Server.Models;

public class PreferenciasNotificacao
{
    public int Id { get; set; }

    public string UtilizadorId { get; set; } = string.Empty;
    public Utilizador? Utilizador { get; set; }

    /// <summary>Ativa/desativa notificações de novas estreias.</summary>
    public bool NovaEstreiaAtiva { get; set; } = true;

    /// <summary>Frequência para geração de notificações de novas estreias: Imediata, Diaria, Semanal.</summary>
    public string NovaEstreiaFrequencia { get; set; } = "Diaria";

    public DateTime AtualizadaEm { get; set; } = DateTime.UtcNow;
}

