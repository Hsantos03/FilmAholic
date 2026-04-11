using System.ComponentModel.DataAnnotations;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa um voto em um post de uma comunidade.
    /// </summary>
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
