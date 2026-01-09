using Microsoft.EntityFrameworkCore;
using FilmAholic.Server.Models;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : DbContext
{
    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }

    public DbSet<Utilizador> Utilizadores => Set<Utilizador>();
    public DbSet<Filme> Filmes => Set<Filme>();
}
