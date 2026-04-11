using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models
{
    /// <summary>
    /// Representa a relação entre um utilizador e um filme.
    /// </summary>
    public class UserMovie
    {
        public int Id { get; set; }

        public string UtilizadorId { get; set; }

        [JsonIgnore]
        public Utilizador Utilizador { get; set; }

        public int FilmeId { get; set; }
        public Filme Filme { get; set; }

        public bool JaViu { get; set; } // false = Quero Ver | true = Já Vi
        public bool Favorito { get; set; } // false = Não é favorito | true = É favorito
        public DateTime Data { get; set; } = DateTime.UtcNow;
    }
}
