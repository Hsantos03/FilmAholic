using FilmAholic.Server.Models;

namespace FilmAholic.Server.Services;

/// <summary>
/// Obtém estatísticas de visualização de filmes por género.
/// </summary>
public static class WatchStatisticsHelper
{
    public static List<(string genero, int total)> CountByIndividualGenre(IEnumerable<UserMovie> movies)
    {
        return movies
            .Where(m => m.Filme != null && !string.IsNullOrWhiteSpace(m.Filme.Genero))
            .SelectMany(m => m.Filme!.Genero
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s)))
            .GroupBy(g => g)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }
}
