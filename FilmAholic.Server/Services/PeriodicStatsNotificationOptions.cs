namespace FilmAholic.Server.Services;

/// <summary>
/// Opções de configuração para notificações periódicas de estatísticas.
/// </summary>
public class PeriodicStatsNotificationOptions
{
    public bool Enabled { get; set; } = true;
    public int HourUtc { get; set; } = 8;
    public int MinuteUtc { get; set; }
    /// Só utilizadores com pelo menos uma marcação “já vi” neste intervalo recebem o resumo.
    public int RecentActivityDays { get; set; } = 14;
    /// Janela para “filme mais assistido da semana” (comunidade).
    public int CommunityWeekDays { get; set; } = 7;
}
