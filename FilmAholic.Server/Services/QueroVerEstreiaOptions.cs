namespace FilmAholic.Server.Services;

/// Configuração do job periódico que verifica lista "Quero ver" vs cinema/streaming (FR63).
public sealed class QueroVerEstreiaOptions
{
    public bool Enabled { get; set; } = true;

    /// Intervalo entre execuções em minutos (1–1440).
    public int IntervalMinutes { get; set; } = 60;
}