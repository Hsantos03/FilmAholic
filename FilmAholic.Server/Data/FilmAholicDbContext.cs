using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : IdentityDbContext<Utilizador>
{
    public DbSet<UserMovie> UserMovies { get; set; }

    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }

    public DbSet<Filme> Filmes => Set<Filme>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<UserMovie>()
        .HasIndex(um => new { um.UtilizadorId, um.FilmeId })
        .IsUnique();
        base.OnModelCreating(builder);
    }
}
