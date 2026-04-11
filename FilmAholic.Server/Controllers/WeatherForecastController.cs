using Microsoft.AspNetCore.Mvc;

namespace FilmAholic.Server.Controllers
{
    /// <summary>
    /// Controlador para fornecer previsões meteorológicas simuladas.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        /// <summary>
        /// Construtor do controlador WeatherForecast.
        /// </summary>
        /// <param name="logger">Registo de rotinas (ILogger).</param>
        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Recupera previsões meteorológicas simuladas para os próximos cinco dias.
        /// </summary>
        /// <returns>Uma matriz iterativa dos objetos WeatherForecast gerados de forma randómica para cinco dias.</returns>
        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
