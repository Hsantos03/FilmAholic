using System;
using System.Collections.Generic;

namespace FilmAholic.Server.Models;

public class Comunidade
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? Descricao { get; set; }
    public int? LimiteMembros { get; set; }
    public bool IsPrivada { get; set; }

    /// Ficheiro guardado em wwwroot/uploads/comunidades.
    public string? BannerFileName { get; set; }

    /// Ficheiro do ícone/foto de perfil guardado em wwwroot/uploads/comunidades/icons/
    public string? IconFileName { get; set; }

    public string? CreatedById { get; set; } 
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

    public ICollection<ComunidadeMembro> Membros { get; set; } = new List<ComunidadeMembro>();
    public ICollection<ComunidadePost> Posts { get; set; } = new List<ComunidadePost>();
    public ICollection<ComunidadePedidoEntrada> PedidosEntrada { get; set; } = new List<ComunidadePedidoEntrada>();
}