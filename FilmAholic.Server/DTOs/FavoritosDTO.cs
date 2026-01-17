namespace FilmAholic.Server.DTOs
{
    public class FavoritosDTO
    {
        public List<int> Filmes { get; set; } = new();
        public List<string> Atores { get; set; } = new();
    }
}
