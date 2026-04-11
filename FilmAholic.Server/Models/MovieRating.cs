using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa a avaliação de um filme por um utilizador.
    /// </summary>
    public class MovieRating
    {
        public int Id { get; set; }

        public int FilmeId { get; set; }
        public Filme? Filme { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(0, 10)]
        public int Score { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
