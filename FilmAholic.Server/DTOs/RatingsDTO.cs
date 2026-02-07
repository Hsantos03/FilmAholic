namespace FilmAholic.Server.DTOs
{
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