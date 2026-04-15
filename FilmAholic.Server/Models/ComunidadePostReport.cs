using System;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa um relatório de um post em uma comunidade.
    /// </summary>
    public class ComunidadePostReport
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UtilizadorId { get; set; } = "";
        public DateTime DataReport { get; set; } = DateTime.UtcNow;

        public ComunidadePost Post { get; set; } = null!;
    }
}
