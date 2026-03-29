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

    /// <summary>Resumo periódico de estatísticas (FR70).</summary>
    public bool ResumoEstatisticasAtiva { get; set; } = true;

    /// <summary>Frequência do resumo: Diaria ou Semanal.</summary>
    public string ResumoEstatisticasFrequencia { get; set; } = "Semanal";

    public DateTime AtualizadaEm { get; set; } = DateTime.UtcNow;
}

