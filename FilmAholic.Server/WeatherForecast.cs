namespace FilmAholic.Server
{
    /// <summary>
    /// Representa uma previsão meteorológica para um dia específico.
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// A data � qual esta previs�o meteorol�gica pertence.
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// Temperatura prevista medida em graus Celsius.
        /// </summary>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperatura prevista convertida e calculada em graus Fahrenheit.
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Resumo textual curto da condi��o clim�tica em vigor (ex: Quente, Frio, Ameno).
        /// </summary>
        public string? Summary { get; set; }
    }
}
