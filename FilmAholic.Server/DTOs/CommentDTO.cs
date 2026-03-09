namespace FilmAholic.Server.DTOs
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? FotoPerfilUrl { get; set; }
        public string Texto { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataEdicao { get; set; }
        public bool CanEdit { get; set; }

        // Votes - Likes/Dislikes/Contagem dos Comments
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int MyVote { get; set; }
    }
}
