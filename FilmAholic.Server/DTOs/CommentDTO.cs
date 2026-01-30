namespace FilmAholic.Server.DTOs
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
