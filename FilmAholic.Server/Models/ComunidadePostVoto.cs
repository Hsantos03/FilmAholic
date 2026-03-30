using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    public class ComunidadePostVoto
    {
        public int Id { get; set; }
        
        [Required]
        public int PostId { get; set; }
        
        [Required]
        public string UtilizadorId { get; set; } = "";
        
        public bool IsLike { get; set; } // true = like, false = dislike

        public ComunidadePost Post { get; set; } = null!;
    }
}
