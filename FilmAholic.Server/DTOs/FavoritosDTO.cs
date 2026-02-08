using System.Text.Json.Serialization;

namespace FilmAholic.Server.DTOs
{
    public class FavoritosDTO
    {
        [JsonPropertyName("filmes")]
        public List<int> Filmes { get; set; } = new();

        [JsonPropertyName("atores")]
        public List<string> Atores { get; set; } = new();
    }
}
