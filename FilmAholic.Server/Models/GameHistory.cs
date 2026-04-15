using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa o histórico de um jogo.
    /// </summary>
    public class GameHistory
    {
        public int Id { get; set; }

        [Required]
        public string UtilizadorId { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public int Score { get; set; }

        // JSON payload with details of rounds, e.g. [{ leftId:1, rightId:2, chosen:"left", correct:"left", leftRating:7.5, rightRating:6.2 }, ...]
        [Required]
        public string RoundsJson { get; set; } = string.Empty;

        public string Category { get; set; } = "films";
    }
}