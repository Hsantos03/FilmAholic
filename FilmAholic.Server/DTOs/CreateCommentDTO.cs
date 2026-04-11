namespace FilmAholic.Server.DTOs
{
    /// <summary>
    /// Representa os dados necessários para criar um comentário, incluindo informações sobre o filme, texto do comentário e votos.
    /// </summary>
    public class CreateCommentDTO
    {
        // Update Comments
        public int FilmeId { get; set; }
        public string Texto { get; set; } = string.Empty;


        // Likes/Dislikes/Contagem dos Comments
        public int Value { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int MyVote { get; set; }
    }
}
