using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : IdentityDbContext<Utilizador>
{
    public DbSet<UserMovie> UserMovies { get; set; }

    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }
    public DbSet<Comments> Comments { get; set; }
    public DbSet<MovieRating> MovieRatings { get; set; }
    public DbSet<CommentVote> CommentVotes { get; set; }
    public DbSet<Filme> Filmes => Set<Filme>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<UtilizadorGenero> UtilizadorGeneros => Set<UtilizadorGenero>();
    public DbSet<Desafio> Desafios => Set<Desafio>();

    // NEW: UserDesafios table to track user progress on desafios
    public DbSet<UserDesafio> UserDesafios => Set<UserDesafio>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<UserMovie>()
            .HasIndex(um => new { um.UtilizadorId, um.FilmeId })
            .IsUnique();

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

       builder.Entity<MovieRating>()
        .HasIndex(r => new { r.FilmeId, r.UserId })
        .IsUnique();

        builder.Entity<MovieRating>()
            .HasOne(r => r.Filme)
            .WithMany()
            .HasForeignKey(r => r.FilmeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Compatibilidade: tabela Comments sem coluna DataEdicao; coluna do filme usa nome EF por defeito (FilmeId)
        builder.Entity<Comments>(e =>
        {
            e.Ignore(c => c.DataEdicao);
        });

        builder.Entity<CommentVote>()
            .HasIndex(v => new { v.CommentId, v.UserId })
            .IsUnique();

        builder.Entity<CommentVote>()
            .HasOne(v => v.Comment)
            .WithMany()
            .HasForeignKey(v => v.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optional: basic configuration for Desafio
        builder.Entity<Desafio>(e =>
        {
            e.Property(d => d.Descricao).HasMaxLength(2000);
            e.Property(d => d.Genero).HasMaxLength(150);
            e.HasIndex(d => d.Ativo);
        });

        // Configure UserDesafio: unique per user + desafio, and FKs
        builder.Entity<UserDesafio>(ud =>
        {
            ud.HasIndex(x => new { x.UtilizadorId, x.DesafioId }).IsUnique();

            ud.HasOne(x => x.Utilizador)
              .WithMany() // no navigation required on Utilizador side; add if you add collection there
              .HasForeignKey(x => x.UtilizadorId)
              .OnDelete(DeleteBehavior.Cascade);

            ud.HasOne(x => x.Desafio)
              .WithMany() // no navigation required on Desafio side; add if you add collection there
              .HasForeignKey(x => x.DesafioId)
              .OnDelete(DeleteBehavior.Cascade);

            ud.Property(x => x.QuantidadeProgresso).HasDefaultValue(0);
            ud.Property(x => x.DataAtualizacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
