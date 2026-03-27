FilmAholic.Server\Models\ComunidadePost.cs
using System;

namespace FilmAholic.Server.Models;

public class ComunidadePost
{
    public int Id { get; set; }
    public int ComunidadeId { get; set; }
    public string? UtilizadorId { get; set; } // nullable to allow soft-deleted users
    public string Titulo { get; set; } = "";
    public string Conteudo { get; set; } = "";
    public string? ImagemUrl { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataAtualizacao { get; set; }

    public Comunidade Comunidade { get; set; } = null!;
    public Utilizador? Utilizador { get; set; }
}