using FilmAholic.Server.Data;
using FilmAholic.Server.DTOs;
using FilmAholic.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Services
{
    public class MedalhaService
    {
        private readonly FilmAholicDbContext _context;

        public MedalhaService(FilmAholicDbContext context)
        {
            _context = context;
        }

        private async Task AtribuirMedalha(string userId, int medalhaId)
        {
            var registo = new UtilizadorMedalha
            {
                UtilizadorId = userId,
                MedalhaId = medalhaId,
                DataConquista = DateTime.UtcNow
            };

            _context.UtilizadorMedalhas.Add(registo);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Medalha>> VerificarConquistasFilmeVisto(string userId)
        {
            return await VerificarConquistas(userId, "filmesVistos");
        }

        public async Task<List<Medalha>> VerificarConquistasNivel(string userId)
        {
            return await VerificarConquistas(userId, "nivel");
        }

        public async Task<List<Medalha>> VerificarMedalhasNivel(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return new List<Medalha>();

            return await VerificarConquistas(userId, "nivel");
        }

        public async Task<List<Medalha>> VerificarConquistasDesafios(string userId)
        {
            return await VerificarConquistas(userId, "desafiosDiarios");
        }

        public async Task<List<Medalha>> VerificarConquistasHigherOrLower(string userId)
        {
            return await VerificarConquistas(userId, "higherOrLower");
        }

        public async Task<List<Medalha>> VerificarConquistasComunidades(string userId)
        {
            var medalhasNovas = new List<Medalha>();
            
            medalhasNovas.AddRange(await VerificarConquistas(userId, "criarComunidade"));
            
            medalhasNovas.AddRange(await VerificarConquistas(userId, "juntarComunidade"));
            
            return medalhasNovas.Distinct().ToList();
        }

        public async Task<List<Medalha>> VerificarTodasConquistas(string userId)
        {
            var todasMedalhasNovas = new List<Medalha>();
            
            todasMedalhasNovas.AddRange(await VerificarConquistasFilmeVisto(userId));
            todasMedalhasNovas.AddRange(await VerificarConquistasNivel(userId));
            todasMedalhasNovas.AddRange(await VerificarConquistasDesafios(userId));
            todasMedalhasNovas.AddRange(await VerificarConquistasHigherOrLower(userId));
            todasMedalhasNovas.AddRange(await VerificarConquistasComunidades(userId));
            
            return todasMedalhasNovas.Distinct().ToList();
        }

        public async Task<List<Medalha>> VerificarConquistas(string userId, string criterioTipo)
        {
            var medalhasNovas = new List<Medalha>();

            var medalhas = await _context.Medalhas
                .Where(m => m.CriterioTipo == criterioTipo && m.Ativa)
                .ToListAsync();

            int count = criterioTipo switch
            {
                "avaliacoes" => await _context.MovieRatings.CountAsync(a => a.UserId == userId),
                "comentarios" => await _context.Comments.CountAsync(c => c.UserId == userId),
                "favoritos" => await _context.UserMovies.CountAsync(f => f.UtilizadorId == userId && f.Favorito),
                "filmesVistos" => await _context.UserMovies.CountAsync(f => f.UtilizadorId == userId && f.JaViu),
                "nivel" => await _context.Users.Where(u => u.Id == userId).Select(u => u.Nivel).FirstOrDefaultAsync(),
                "desafiosDiarios" => await _context.UserDesafios.CountAsync(ud => ud.UtilizadorId == userId && ud.Respondido && ud.Acertou),
                "higherOrLower" => await _context.GameHistories.CountAsync(gh => gh.UtilizadorId == userId),
                "criarComunidade" => await _context.ComunidadeMembros.CountAsync(cm => cm.UtilizadorId == userId && cm.Role == "Admin"),
                "juntarComunidade" => await _context.ComunidadeMembros.CountAsync(cm => cm.UtilizadorId == userId),
                _ => 0
            };

            var jaConquistadas = await _context.UtilizadorMedalhas
                .Where(um => um.UtilizadorId == userId)
                .Select(um => um.MedalhaId)
                .ToListAsync();

            foreach (var medalha in medalhas)
            {
                if (count >= medalha.CriterioQuantidade && !jaConquistadas.Contains(medalha.Id))
                {
                    await AtribuirMedalha(userId, medalha.Id);

                    medalhasNovas.Add(new Medalha
                    {
                        Id = medalha.Id,
                        Nome = medalha.Nome,
                        Descricao = medalha.Descricao,
                        IconeUrl = medalha.IconeUrl,
                        CriterioTipo = medalha.CriterioTipo,
                        CriterioQuantidade = medalha.CriterioQuantidade,
                        Ativa = medalha.Ativa
                    });
                }
            }

            return medalhasNovas;
        }

        public async Task<List<object>> GetMedalhasDoUtilizador(string userId)
        {
            return await _context.UtilizadorMedalhas
                .Where(um => um.UtilizadorId == userId)
                .Include(um => um.Medalha)
                .OrderByDescending(um => um.DataConquista)
                .Select(um => new {
            dataConquista = um.DataConquista,
            medalha = new {
                nome = um.Medalha.Nome,
                descricao = um.Medalha.Descricao,
                iconeUrl = um.Medalha.IconeUrl
            }
        })
                .ToListAsync<object>();
        }

        public async Task<List<MedalhaProgressoDto>> GetTodasComProgresso(string userId)
        {
            var todas = await _context.Medalhas.Where(m => m.Ativa).ToListAsync();

            var conquistadas = await _context.UtilizadorMedalhas
                .Where(um => um.UtilizadorId == userId)
                .Include(um => um.Medalha)
                .ToListAsync();

            return todas.Select(m => {
                var conquistada = conquistadas.FirstOrDefault(c => c.MedalhaId == m.Id);
                return new MedalhaProgressoDto
                {
                    Id = m.Id,
                    Nome = m.Nome,
                    Descricao = m.Descricao,
                    IconeUrl = m.IconeUrl,
                    Conquistada = conquistada != null,
                    DataConquista = conquistada?.DataConquista
                };
            }).ToList();
        }
    }
}
