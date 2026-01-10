using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : IdentityDbContext<Utilizador>
{
    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }

    public DbSet<Filme> Filmes => Set<Filme>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Aqui podes adicionar configurações extras se o teu colega precisar
    }
}
