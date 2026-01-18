using System;
using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models
{
    public class UserDesafio
    {
        public int Id { get; set; }

        // FK to AspNetUsers (Utilizador)
        public string UtilizadorId { get; set; }

        [JsonIgnore]
        public Utilizador Utilizador { get; set; }

        // FK to Desafio
        public int DesafioId { get; set; }
        public Desafio Desafio { get; set; }

        // Quantity the user has progressed towards completing the desafio
        public int QuantidadeProgresso { get; set; } = 0;

        // When the progress was last updated
        public DateTime DataAtualizacao { get; set; } = DateTime.Now;
    }
}