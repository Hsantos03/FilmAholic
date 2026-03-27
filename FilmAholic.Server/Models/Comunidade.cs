using System;
using System.Collections.Generic;

namespace FilmAholic.Server.Models;

public class Comunidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public string? CreatedById { get; set; } // optional FK to Utilizador
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<ComunidadeMembro> Membros { get; set; } = new List<ComunidadeMembro>();
    public ICollection<ComunidadePost> Posts { get; set; } = new List<ComunidadePost>();
}