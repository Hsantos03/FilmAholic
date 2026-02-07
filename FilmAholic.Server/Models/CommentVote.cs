using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    public class CommentVote
    {
        public int Id { get; set; }

        public int CommentId { get; set; }
        public Comments? Comment { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsLike { get; set; }

        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
        public DateTime DataAtualizacao { get; set; } = DateTime.UtcNow;
    }
}
