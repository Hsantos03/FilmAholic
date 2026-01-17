using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models
{
    public class UserMovie
    {
        public int Id { get; set; }

        public string UtilizadorId { get; set; }

        [JsonIgnore]
        public Utilizador Utilizador { get; set; }

        public int FilmeId { get; set; }
        public Filme Filme { get; set; }

        public bool JaViu { get; set; } // false = Quero Ver | true = Já Vi
        public DateTime Data { get; set; } = DateTime.Now;
    }
}
