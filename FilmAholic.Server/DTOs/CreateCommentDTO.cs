namespace FilmAholic.Server.DTOs
{
    public class CreateCommentDTO
    {
        public int FilmeId { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
