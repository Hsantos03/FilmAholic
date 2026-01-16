using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services;

public interface IPreferenciasService
{
    Task<List<Genero>> ObterTodosGenerosAsync();
    Task<List<Genero>> ObterGenerosFavoritosAsync(string utilizadorId);
    Task AtualizarGenerosFavoritosAsync(string utilizadorId, List<int> generoIds);
    Task<bool> GeneroExisteAsync(int generoId);
}

public class PreferenciasService : IPreferenciasService
{
    private readonly FilmAholicDbContext _context;
    private readonly ILogger<PreferenciasService> _logger;

    public PreferenciasService(FilmAholicDbContext context, ILogger<PreferenciasService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Genero>> ObterTodosGenerosAsync()
    {
        return await _context.Generos
            .OrderBy(g => g.Nome)
            .ToListAsync();
    }

    public async Task<List<Genero>> ObterGenerosFavoritosAsync(string utilizadorId)
    {
        return await _context.UtilizadorGeneros
            .Where(ug => ug.UtilizadorId == utilizadorId)
            .Include(ug => ug.Genero)
            .Select(ug => ug.Genero)
            .OrderBy(g => g.Nome)
            .ToListAsync();
    }

    public async Task AtualizarGenerosFavoritosAsync(string utilizadorId, List<int> generoIds)
    {
        // Remover géneros favoritos existentes
        var generosExistentes = await _context.UtilizadorGeneros
            .Where(ug => ug.UtilizadorId == utilizadorId)
            .ToListAsync();

        _context.UtilizadorGeneros.RemoveRange(generosExistentes);

        // Adicionar novos géneros favoritos
        if (generoIds != null && generoIds.Any())
        {
            // Verificar se todos os géneros existem
            var generosValidos = await _context.Generos
                .Where(g => generoIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync();

            var generosParaAdicionar = generosValidos.Select(generoId => new UtilizadorGenero
            {
                UtilizadorId = utilizadorId,
                GeneroId = generoId,
                DataAdicao = DateTime.UtcNow
            }).ToList();

            _context.UtilizadorGeneros.AddRange(generosParaAdicionar);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> GeneroExisteAsync(int generoId)
    {
        return await _context.Generos.AnyAsync(g => g.Id == generoId);
    }
}
