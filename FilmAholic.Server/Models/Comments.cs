using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa um comentário de um utilizador.
    /// </summary>
    public class Comments
    {
        public int Id { get; set; }

        public int FilmeId { get; set; }
        public Filme? Filme { get; set; }

        public string? UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Texto { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataEdicao { get; set; }
    }

}
