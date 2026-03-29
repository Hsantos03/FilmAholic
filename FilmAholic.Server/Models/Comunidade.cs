using System;
using System.Collections.Generic;

namespace FilmAholic.Server.Models;

public class Comunidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }

    /// Ficheiro guardado em wwwroot/uploads/comunidades.
    public string? BannerFileName { get; set; }
    public string? CreatedById { get; set; } // FK para Utilizador.Id; se null, comunidade foi criada por um processo automático ou admin sem conta de utilizador associada
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<ComunidadeMembro> Membros { get; set; } = new List<ComunidadeMembro>();
    public ICollection<ComunidadePost> Posts { get; set; } = new List<ComunidadePost>();
}