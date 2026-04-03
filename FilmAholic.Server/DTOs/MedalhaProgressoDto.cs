namespace FilmAholic.Server.DTOs
{
    public class MedalhaProgressoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string IconeUrl { get; set; } = string.Empty;
        public bool Conquistada { get; set; }
        public DateTime? DataConquista { get; set; }
        public int Progresso { get; set; }
    }
}
