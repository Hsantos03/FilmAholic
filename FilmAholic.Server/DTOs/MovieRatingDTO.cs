namespace FilmAholic.Server.DTOs
{
    public class MovieRatingDTO
    {
        public double Average { get; set; }
        public int Count { get; set; }
        public int? UserScore { get; set; }
    }
}
