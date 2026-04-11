namespace FilmAholic.Server.Models;

/// <summary>
/// Representa um desafio.
/// </summary>
public class Desafio
{
    public int Id { get; set; }

    // Start and end date for the challenge
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }

    // Description text
    public string Descricao { get; set; } = "";

    // Is the challenge currently active
    public bool Ativo { get; set; } = false;

    // Target film category/genre for the challenge
    public string Genero { get; set; } = "";

    // Quantity of films needed to complete the challenge
    public int QuantidadeNecessaria { get; set; }

    // XP awarded to the user upon completion
    public int Xp { get; set; }

    // Quiz properties for daily challenge
    public string Pergunta { get; set; } = "";
    public string OpcaoA { get; set; } = "";
    public string OpcaoB { get; set; } = "";
    public string OpcaoC { get; set; } = "";
    public string RespostaCorreta { get; set; } = ""; // "A", "B" or "C"
}