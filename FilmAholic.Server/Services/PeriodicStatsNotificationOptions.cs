namespace FilmAholic.Server.Services;

public class PeriodicStatsNotificationOptions
{
    public bool Enabled { get; set; } = true;
    public int HourUtc { get; set; } = 8;
    public int MinuteUtc { get; set; }
    /// <summary>Só utilizadores com pelo menos uma marcação “já vi” neste intervalo recebem o resumo.</summary>
    public int RecentActivityDays { get; set; } = 14;
    /// <summary>Janela para “filme mais assistido da semana” (comunidade).</summary>
    public int CommunityWeekDays { get; set; } = 7;
}
