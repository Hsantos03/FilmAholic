using System.Text.Json.Serialization;

namespace FilmAholic.Server.DTOs;

public class TmdbPopularPeopleResponse
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("cast")]
    public List<TmdbPersonDto> Cast { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbPersonDto> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

public class TmdbPersonDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("character")]
    public string Character { get; set; } = "";

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }

}

public class PopularActorDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = "";

    [JsonPropertyName("fotoUrl")]
    public string FotoUrl { get; set; } = "";

    [JsonPropertyName("popularidade")]
    public double Popularidade { get; set; }
}

public class CastMemberDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Personagem { get; set; } = "";
    public string? FotoUrl { get; set; }
}