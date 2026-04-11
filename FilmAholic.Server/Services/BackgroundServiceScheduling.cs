namespace FilmAholic.Server.Services;

/// <summary>Utilitários partilhados por <see cref="BackgroundService"/> com agendamento diário (UTC).</summary>
internal static class BackgroundServiceScheduling
{
    internal static TimeSpan DelayUntilNextRunUtc(int hourUtc, int minuteUtc)
    {
        var now = DateTime.UtcNow;
        var h = Math.Clamp(hourUtc, 0, 23);
        var m = Math.Clamp(minuteUtc, 0, 59);
        var next = new DateTime(now.Year, now.Month, now.Day, h, m, 0, DateTimeKind.Utc);
        if (now >= next)
            next = next.AddDays(1);
        return next - now;
    }
}
