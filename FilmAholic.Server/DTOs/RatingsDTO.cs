namespace FilmAholic.Server.DTOs
{
    /// <summary>
    /// Representa as classificações de um filme, incluindo avaliações de diferentes fontes e a pontuação do utilizador.
    /// </summary>
    public class RatingsDto
    {
        // TMDb
        public double? TmdbVoteAverage { get; set; }
        public int? TmdbVoteCount { get; set; }

        // OMDb
        public string? ImdbId { get; set; }
        public string? ImdbRating { get; set; }

        public string? Metascore { get; set; }
        public string? RottenTomatoes { get; set; }

        // User rating
        public int Score { get; set; }
    }
}