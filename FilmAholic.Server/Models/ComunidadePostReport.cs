using System;

namespace FilmAholic.Server.Models
{
    public class ComunidadePostReport
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UtilizadorId { get; set; } = "";
        public DateTime DataReport { get; set; } = DateTime.UtcNow;

        public ComunidadePost Post { get; set; } = null!;
    }
}
