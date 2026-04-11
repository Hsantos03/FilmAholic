using System;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa um comentário em um post de uma comunidade.
    /// </summary>
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
