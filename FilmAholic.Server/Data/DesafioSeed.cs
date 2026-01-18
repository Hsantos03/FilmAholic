using System;
using System.Collections.Generic;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data
{
    public static class DesafioSeed
    {
        public static List<Desafio> Desafios = new()
        {
            new Desafio
            {
                Id = 1,
                DataInicio = new DateTime(2026, 1, 19),
                DataFim = new DateTime(2026, 1, 25),
                Descricao = "Assista a 3 filmes do género Animação esta semana.",
                Ativo = true,
                Genero = "Animação",
                QuantidadeNecessaria = 3,
                Xp = 50
            },
            new Desafio
            {
                Id = 2,
                DataInicio = new DateTime(2026, 1, 26),
                DataFim = new DateTime(2026, 2, 1),
                Descricao = "Assista a 5 filmes do género Comédia na próxima semana.",
                Ativo = false,
                Genero = "Comédia",
                QuantidadeNecessaria = 5,
                Xp = 120
            },
            new Desafio
            {
                Id = 3,
                DataInicio = new DateTime(2025, 12, 1),
                DataFim = new DateTime(2025, 12, 7),
                Descricao = "Assista a 2 filmes do género Ação (desafio passado).",
                Ativo = false,
                Genero = "Ação",
                QuantidadeNecessaria = 2,
                Xp = 30
            }
        };
    }
}