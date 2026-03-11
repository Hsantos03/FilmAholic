using System.Text.Json.Serialization;

namespace FilmAholic.Server.DTOs;

public class TmdbPopularPeopleResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbPersonDto> Results { get; set; } = new();

    [JsonPropertyName("cast")]
    public List<TmdbPersonDto> Cast { get; set; } = new();

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

/// <summary>Response from TMDb GET /search/person</summary>
public class TmdbSearchPersonResponse
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("results")]
    public List<TmdbPersonSearchResult> Results { get; set; } = new();

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("total_results")]
    public int TotalResults { get; set; }
}

public class TmdbPersonSearchResult
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

/// <summary>Response from TMDb GET /person/{id}/movie_credits</summary>
public class TmdbPersonMovieCreditsResponse
{
    [JsonPropertyName("cast")]
    public List<TmdbCastMovieDto> Cast { get; set; } = new();
}

public class TmdbCastMovieDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("poster_path")]
    public string? PosterPath { get; set; }

    [JsonPropertyName("character")]
    public string? Character { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }
}

/// <summary>Response from TMDb GET /person/{id}</summary>
public class TmdbPersonDetailsDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("biography")]
    public string? Biography { get; set; }

    [JsonPropertyName("birthday")]
    public string? Birthday { get; set; }

    [JsonPropertyName("place_of_birth")]
    public string? PlaceOfBirth { get; set; }

    [JsonPropertyName("profile_path")]
    public string? ProfilePath { get; set; }

    [JsonPropertyName("known_for_department")]
    public string? KnownForDepartment { get; set; }
}

/// <summary>API response: actor in search results (same shape as PopularActorDto for client)</summary>
public class ActorSearchResultDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string FotoUrl { get; set; } = "";
}

/// <summary>API response: movie in "movies by actor" list</summary>
public class ActorMovieDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? PosterUrl { get; set; }
    public string? Personagem { get; set; }
    public string? DataLancamento { get; set; }
}

/// <summary>API response: actor details page</summary>
public class ActorDetailsDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? FotoUrl { get; set; }
    public string? Biografia { get; set; }
    public string? DataNascimento { get; set; }
    public string? LocalNascimento { get; set; }
    public string? Departamento { get; set; }
}
