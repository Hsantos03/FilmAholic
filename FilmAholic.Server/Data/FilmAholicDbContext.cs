using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Data;

public class FilmAholicDbContext : IdentityDbContext<Utilizador>
{
    public DbSet<UserMovie> UserMovies { get; set; }
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();
    public DbSet<PreferenciasNotificacao> PreferenciasNotificacao => Set<PreferenciasNotificacao>();

    public FilmAholicDbContext(DbContextOptions<FilmAholicDbContext> options) : base(options) { }
    public DbSet<Comments> Comments { get; set; }
    public DbSet<MovieRating> MovieRatings { get; set; }
    public DbSet<CommentVote> CommentVotes { get; set; }
    public DbSet<Filme> Filmes => Set<Filme>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<UtilizadorGenero> UtilizadorGeneros => Set<UtilizadorGenero>();
    public DbSet<Desafio> Desafios => Set<Desafio>();
    public DbSet<UserDesafio> UserDesafios => Set<UserDesafio>();
    public DbSet<GameHistory> GameHistories => Set<GameHistory>();

    public DbSet<CinemaMovieCache> CinemaMovieCache => Set<CinemaMovieCache>();

    public DbSet<Comunidade> Comunidades => Set<Comunidade>();
    public DbSet<ComunidadeMembro> ComunidadeMembros => Set<ComunidadeMembro>();
    public DbSet<ComunidadePedidoEntrada> ComunidadePedidosEntrada => Set<ComunidadePedidoEntrada>();
    public DbSet<ComunidadePost> ComunidadePosts => Set<ComunidadePost>();
    public DbSet<ComunidadePostVoto> ComunidadePostVotos => Set<ComunidadePostVoto>();
    public DbSet<ComunidadePostReport> ComunidadePostReports => Set<ComunidadePostReport>();
    public DbSet<ComunidadePostComentario> ComunidadePostComentarios => Set<ComunidadePostComentario>();

    public DbSet<RecomendacaoFeedback> RecomendacaoFeedbacks => Set<RecomendacaoFeedback>();

    public DbSet<NotificacaoComunidade> NotificacoesComunidade => Set<NotificacaoComunidade>();

    public DbSet<Medalha> Medalhas => Set<Medalha>();
    public DbSet<UtilizadorMedalha> UtilizadorMedalhas => Set<UtilizadorMedalha>();
    public DbSet<UtilizadorMedalhaExposicao> UtilizadorMedalhasExposicao => Set<UtilizadorMedalhaExposicao>();

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
            // Allow UserId to be nullable for deleted accounts
            e.Property(c => c.UserId).IsRequired(false);
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
            e.Property(d => d.Pergunta).HasMaxLength(1000);
            e.Property(d => d.OpcaoA).HasMaxLength(500);
            e.Property(d => d.OpcaoB).HasMaxLength(500);
            e.Property(d => d.OpcaoC).HasMaxLength(500);
            e.Property(d => d.RespostaCorreta).HasMaxLength(1);
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
            ud.Property(x => x.Respondido).HasDefaultValue(false);
            ud.Property(x => x.Acertou).HasDefaultValue(false);
        });

        // Configure GameHistory basic mapping
        builder.Entity<GameHistory>(gh =>
        {
            gh.HasKey(x => x.Id);
            gh.Property(x => x.UtilizadorId).IsRequired();
            gh.Property(x => x.RoundsJson).IsRequired();
            gh.HasIndex(x => x.UtilizadorId);
            gh.ToTable("GameHistories");
        });

        // Configure CinemaMovieCache
        builder.Entity<CinemaMovieCache>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(500);
            e.Property(x => x.Cinema).HasMaxLength(100);
            e.Property(x => x.MovieId).HasMaxLength(100);
            e.Property(x => x.Poster).HasMaxLength(1000);
            e.Property(x => x.HorariosJson).HasMaxLength(2000);
            e.Property(x => x.Genero).HasMaxLength(100);
            e.Property(x => x.Duracao).HasMaxLength(50);
            e.Property(x => x.Classificacao).HasMaxLength(50);
            e.Property(x => x.Idioma).HasMaxLength(50);
            e.Property(x => x.Sala).HasMaxLength(100);
            e.Property(x => x.Link).HasMaxLength(1000);
            e.HasIndex(x => x.DataCache);
            e.ToTable("CinemaMovieCache");
        });

        // NEW: Comunidade mapping
        builder.Entity<Comunidade>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Nome).IsRequired().HasMaxLength(200);
            e.Property(c => c.Descricao).HasMaxLength(2000);
            e.Property(c => c.BannerFileName).HasMaxLength(512);
            e.Property(c => c.IconFileName).HasMaxLength(512);
            e.Property(c => c.LimiteMembros).IsRequired(false);
            e.Property(c => c.IsPrivada).HasDefaultValue(false);
            e.Property(c => c.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(c => c.Nome).IsUnique();
            e.HasMany(c => c.Membros)
             .WithOne(m => m.Comunidade)
             .HasForeignKey(m => m.ComunidadeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Posts)
             .WithOne(p => p.Comunidade)
             .HasForeignKey(p => p.ComunidadeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.PedidosEntrada)
             .WithOne(p => p.Comunidade)
             .HasForeignKey(p => p.ComunidadeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.ToTable("Comunidades");
        });

        // NEW: ComunidadeMembro mapping
        builder.Entity<ComunidadeMembro>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.UtilizadorId, m.ComunidadeId }).IsUnique();
            e.Property(m => m.Role).IsRequired().HasMaxLength(50);
            e.Property(m => m.Status).IsRequired().HasMaxLength(50);
            e.Property(m => m.MotivoBan).HasMaxLength(500);
            e.Property(m => m.DataEntrada).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // FK to Utilizador; if user removed, cascade to remove membership
            e.HasOne(m => m.Utilizador)
             .WithMany()
             .HasForeignKey(m => m.UtilizadorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.ToTable("ComunidadeMembros");
        });

        builder.Entity<ComunidadePedidoEntrada>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Status).IsRequired().HasMaxLength(30);
            e.Property(p => p.DataPedido).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(p => new { p.ComunidadeId, p.UtilizadorId, p.Status });

            e.HasOne(p => p.Utilizador)
             .WithMany()
             .HasForeignKey(p => p.UtilizadorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.ToTable("ComunidadePedidosEntrada");
        });

        // NEW: ComunidadePost mapping
        builder.Entity<ComunidadePost>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Titulo).IsRequired().HasMaxLength(300);
            e.Property(p => p.Conteudo).IsRequired().HasMaxLength(5000);
            e.Property(p => p.ImagemUrl).HasMaxLength(1000);
            e.Property(p => p.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(p => p.DataAtualizacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(p => p.ComunidadeId);

            // allow posts to remain if user deleted; keep UtilizadorId nullable and set FK to SetNull
            e.HasOne(p => p.Utilizador)
             .WithMany()
             .HasForeignKey(p => p.UtilizadorId)
             .OnDelete(DeleteBehavior.SetNull);

            e.ToTable("ComunidadePosts");
        });

        // NEW: ComunidadePostVoto mapping
        builder.Entity<ComunidadePostVoto>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasIndex(v => new { v.PostId, v.UtilizadorId }).IsUnique();

            e.HasOne(v => v.Post)
             .WithMany()
             .HasForeignKey(v => v.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            e.ToTable("ComunidadePostVotos");
        });

        // NEW: ComunidadePostReport mapping
        builder.Entity<ComunidadePostReport>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.PostId, r.UtilizadorId }).IsUnique();

            e.HasOne(r => r.Post)
             .WithMany()
             .HasForeignKey(r => r.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            e.ToTable("ComunidadePostReports");
        });

        // NEW: ComunidadePostComentario mapping
        builder.Entity<ComunidadePostComentario>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Conteudo).IsRequired().HasMaxLength(2000);
            e.Property(c => c.DataCriacao).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.HasIndex(c => c.PostId);

            e.HasOne(c => c.Post)
             .WithMany()
             .HasForeignKey(c => c.PostId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.Utilizador)
             .WithMany()
             .HasForeignKey(c => c.UtilizadorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.ToTable("ComunidadePostComentarios");
        });

        // Configure Notificacoes (NovaEstreia, ResumoEstatisticas, etc.)
        builder.Entity<Notificacao>(e =>
        {
            e.ToTable("Notificacoes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).HasMaxLength(64).IsRequired();
            e.Property(x => x.Corpo).HasMaxLength(4000);
            e.Property(x => x.FilmeId).IsRequired(false);

            e.HasOne(x => x.Filme)
                .WithMany()
                .HasForeignKey(x => x.FilmeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.UtilizadorId, x.FilmeId, x.Tipo })
                .IsUnique()
                .HasFilter("[FilmeId] IS NOT NULL");
        });

        builder.Entity<PreferenciasNotificacao>(e =>
        {
            e.ToTable("PreferenciasNotificacao");
            e.HasKey(x => x.Id);
            e.Property(x => x.NovaEstreiaFrequencia).HasMaxLength(20).IsRequired();
            e.Property(x => x.ResumoEstatisticasFrequencia).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.UtilizadorId).IsUnique();
            e.Property(x => x.AtualizadaEm).IsRequired();

            e.HasOne(x => x.Utilizador)
                .WithMany()
                .HasForeignKey(x => x.UtilizadorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RecomendacaoFeedback mapping
        builder.Entity<RecomendacaoFeedback>(e =>
        {
            e.ToTable("RecomendacaoFeedbacks");
            e.HasKey(x => x.Id);
            e.Property(x => x.UtilizadorId).IsRequired();
            e.HasIndex(x => new { x.UtilizadorId, x.FilmeId }).IsUnique();

            e.HasOne(x => x.Filme)
             .WithMany()
             .HasForeignKey(x => x.FilmeId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificacaoComunidade mapping
        builder.Entity<NotificacaoComunidade>(e =>
        {
            e.ToTable("NotificacoesComunidade");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UtilizadorId, x.LidaEm });
            e.Property(x => x.Tipo).HasMaxLength(40).IsRequired();
            e.Property(x => x.Corpo).HasMaxLength(2000);

            e.HasOne(x => x.Utilizador)
             .WithMany()
             .HasForeignKey(x => x.UtilizadorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Comunidade)
             .WithMany()
             .HasForeignKey(x => x.ComunidadeId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(x => x.Post)
             .WithMany()
             .HasForeignKey(x => x.PostId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Medalha entity
        builder.Entity<Medalha>(e =>
        {
            e.ToTable("Medalhas");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nome).IsRequired().HasMaxLength(200);
            e.Property(x => x.Descricao).IsRequired().HasMaxLength(1000);
            e.Property(x => x.IconeUrl).IsRequired().HasMaxLength(500);
            e.Property(x => x.CriterioTipo).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.Ativa);
            
            // Seed das medalhas base
            e.HasData(MedalhaSeed.Medalhas);
        });

        // Configure UtilizadorMedalha entity (many-to-many relationship)
        builder.Entity<UtilizadorMedalha>(e =>
        {
            e.ToTable("UtilizadorMedalhas");
            e.HasKey(x => new { x.UtilizadorId, x.MedalhaId });
            e.Property(x => x.DataConquista).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Utilizador)
             .WithMany(u => u.UtilizadorMedalhas)
             .HasForeignKey(x => x.UtilizadorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Medalha)
             .WithMany(m => m.UtilizadorMedalhas)
             .HasForeignKey(x => x.MedalhaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure UtilizadorMedalhaExposicao entity (showcased medals)
        builder.Entity<UtilizadorMedalhaExposicao>(e =>
        {
            e.ToTable("UtilizadorMedalhasExposicao");
            e.HasKey(x => new { x.UtilizadorId, x.SlotIndex });
            e.Property(x => x.SlotIndex).HasMaxLength(3); // 0, 1, 2
            e.Property(x => x.Tag).HasMaxLength(100);
            e.Property(x => x.DataAtualizacao).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Utilizador)
             .WithMany()
             .HasForeignKey(x => x.UtilizadorId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Medalha)
             .WithMany()
             .HasForeignKey(x => x.MedalhaId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}