using System.Text.Json.Serialization;

namespace FilmAholic.Server.DTOs;

/// <summary>
/// Representa a resposta de pessoas populares no TMDb.
/// </summary>
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

/// <summary>
/// Representa uma pessoa no TMDb.
/// </summary>
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

/// <summary>
/// Representa um ator popular no TMDb.
/// </summary>
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

/// <summary>
/// Representa um membro do elenco no TMDb.
/// </summary>
public class CastMemberDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Personagem { get; set; } = "";
    public string? FotoUrl { get; set; }
}

/// Response from TMDb GET /search/person
/// <summary>
/// Representa a resposta de pesquisa de pessoas no TMDb.
/// </summary>
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

/// <summary>
/// Representa um resultado de pesquisa de pessoa no TMDb.
/// </summary>
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

/// <summary>
/// Representa os créditos de filmes de uma pessoa no TMDb.
/// </summary>
public class TmdbPersonMovieCreditsResponse
{
    [JsonPropertyName("cast")]
    public List<TmdbCastMovieDto> Cast { get; set; } = new();
}

/// <summary>
/// Representa um crédito de filme de uma pessoa no TMDb.
/// </summary>
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

/// <summary>
/// Representa os detalhes de uma pessoa no TMDb.
/// </summary>
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

    [JsonPropertyName("deathday")]
    public string? Deathday { get; set; }
}

/// <summary>
/// Representa um resultado de pesquisa de ator no TMDb.
/// </summary>
public class ActorSearchResultDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string FotoUrl { get; set; } = "";
    public double Popularidade { get; set; }
}

/// <summary>
/// Representa um crédito de filme de uma pessoa no TMDb.
/// </summary>
public class ActorMovieDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string? PosterUrl { get; set; }
    public string? Personagem { get; set; }
    public string? DataLancamento { get; set; }
}

/// <summary>
/// Representa os detalhes de uma pessoa no TMDb.
/// </summary>
public class ActorDetailsDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string? FotoUrl { get; set; }
    public string? Biografia { get; set; }
    public string? DataNascimento { get; set; }
    public string? LocalNascimento { get; set; }
    public string? Departamento { get; set; }
    public string? DataFalecimento { get; set; }
}
