namespace FilmAholic.Server.DTOs
{
    /// <summary>
    /// Representa o progresso de uma medalha de um utilizador, incluindo informações como ID, nome, descrição, ícone, status de conquista, data de conquista e progresso.
    /// </summary>
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
