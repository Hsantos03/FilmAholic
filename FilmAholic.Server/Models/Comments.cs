using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int FilmeId { get; set; }
        public Filme? Filme { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Texto { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }

}
