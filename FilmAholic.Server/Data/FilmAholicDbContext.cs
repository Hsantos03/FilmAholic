using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : IdentityDbContext<Utilizador>
{
    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }

    public DbSet<Filme> Filmes => Set<Filme>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<UtilizadorGenero> UtilizadorGeneros => Set<UtilizadorGenero>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configurar relação many-to-many entre Utilizador e Genero
        builder.Entity<UtilizadorGenero>()
            .HasKey(ug => new { ug.UtilizadorId, ug.GeneroId });

        builder.Entity<UtilizadorGenero>()
            .HasOne(ug => ug.Utilizador)
            .WithMany(u => u.GenerosFavoritos)
            .HasForeignKey(ug => ug.UtilizadorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UtilizadorGenero>()
            .HasOne(ug => ug.Genero)
            .WithMany(g => g.Utilizadores)
            .HasForeignKey(ug => ug.GeneroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
