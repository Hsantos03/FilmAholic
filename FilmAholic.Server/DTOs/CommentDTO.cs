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

        // User tag (e.g., "Fundador")
        public string? UserTag { get; set; }

        // Medal description explaining how to unlock it
        public string? UserTagDescription { get; set; }

        // Medal icon URL for the user tag
        public string? UserTagIconUrl { get; set; }

        // Tag colors for gradient animation
        public string? UserTagPrimaryColor { get; set; }
        public string? UserTagSecondaryColor { get; set; }

        // Votes - Likes/Dislikes/Contagem dos Comments
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public int MyVote { get; set; }
    }
    public class PaginatedCommentsDTO
    {
        public List<CommentDTO> Comments { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
