namespace FilmAholic.Server.DTOs
{
    /// <summary>
    /// Representa a classificação de um filme, incluindo a média, contagem de avaliações e a pontuação do utilizador.
    /// </summary>
    public class MovieRatingDTO
    {
        public double Average { get; set; }
        public int Count { get; set; }
        public int? UserScore { get; set; }
    }
}
