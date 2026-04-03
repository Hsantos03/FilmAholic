namespace FilmAholic.Server.Models;

public class PreferenciasNotificacao
{
    public int Id { get; set; }

    public string UtilizadorId { get; set; } = string.Empty;
    public Utilizador? Utilizador { get; set; }

    /// Ativa/desativa notificações de novas estreias.
    public bool NovaEstreiaAtiva { get; set; } = true;

    /// Frequência para geração de notificações de novas estreias: Imediata, Diaria, Semanal.
    public string NovaEstreiaFrequencia { get; set; } = "Diaria";

    /// Resumo periódico de estatísticas (FR70).
    public bool ResumoEstatisticasAtiva { get; set; } = true;

    /// Frequência do resumo: Diaria ou Semanal.
    public string ResumoEstatisticasFrequencia { get; set; } = "Semanal";

    /// Reminder para jogar Higher or Lower.
    public bool ReminderJogoAtiva { get; set; } = true;

    public DateTime AtualizadaEm { get; set; } = DateTime.UtcNow;
}

