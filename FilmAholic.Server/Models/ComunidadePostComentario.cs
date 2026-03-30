using System;

namespace FilmAholic.Server.Models
{
    public class ComunidadePostComentario
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string? UtilizadorId { get; set; } = "";
        public string Conteudo { get; set; } = "";
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public ComunidadePost Post { get; set; } = null!;
        public Utilizador? Utilizador { get; set; } = null!;
    }
}
