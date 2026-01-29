using System.Text.Json.Serialization;

namespace FilmAholic.Server.DTOs;

public class TmdbPopularPeopleResponse
{
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

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("popularity")]
    public double Popularity { get; set; }
}

public class PopularActorDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public double Popularidade { get; set; }
}

