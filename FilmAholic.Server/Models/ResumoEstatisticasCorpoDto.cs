namespace FilmAholic.Server.Models;

/// <summary>Conteúdo serializado em JSON em <see cref="Notificacao.Corpo"/> para tipo <c>ResumoEstatisticas</c>.</summary>
public class ResumoEstatisticasCorpoDto
{
    public double TempoTotalHoras { get; set; }
    public List<ResumoGeneroContagemDto> GenerosMaisVistos { get; set; } = new();
    public ResumoFilmeComunidadeDto? FilmeMaisVistoSemanaPlataforma { get; set; }
}

public class ResumoGeneroContagemDto
{
    public string Nome { get; set; } = string.Empty;
    public int Filmes { get; set; }
}

public class ResumoFilmeComunidadeDto
{
    public int FilmeId { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int MarcacoesNaSemana { get; set; }
}
