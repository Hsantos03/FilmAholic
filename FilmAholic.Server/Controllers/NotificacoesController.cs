using System.Security.Claims;
using System.Text.Json;
using FilmAholic.Server.Data;
using FilmAholic.Server.Models;
using FilmAholic.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmAholic.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificacoesController : ControllerBase
    {
        private const string TipoNovaEstreia = "NovaEstreia";
        private const string TipoFilmeDisponivel = "FilmeDisponivel";
        private const string TipoResumoEstatisticas = "ResumoEstatisticas";
        private static readonly string[] FrequenciasPermitidas = ["Imediata", "Diaria", "Semanal"];
        private static readonly string[] ResumoFrequenciasPermitidas = ["Diaria", "Semanal"];
        private readonly FilmAholicDbContext _context;
        private readonly IMovieService _movieService;
        private const string TipoReminderJogo = "ReminderJogo";

        public NotificacoesController(FilmAholicDbContext context, IMovieService movieService)
        {
            _context = context;
            _movieService = movieService;
        }

        private string? GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        }

        private async Task<PreferenciasNotificacao> GetOrCreatePreferenciasNotificacaoAsync(string userId)
        {
            var prefs = await _context.PreferenciasNotificacao
                .FirstOrDefaultAsync(x => x.UtilizadorId == userId);

            if (prefs != null) return prefs;

            prefs = new PreferenciasNotificacao
            {
                UtilizadorId = userId,
                NovaEstreiaAtiva = true,
                NovaEstreiaFrequencia = "Diaria",
                ResumoEstatisticasAtiva = true,
                ResumoEstatisticasFrequencia = "Semanal",
                FilmeDisponivelAtiva = true,
                AtualizadaEm = DateTime.UtcNow
            };

            _context.PreferenciasNotificacao.Add(prefs);
            await _context.SaveChangesAsync();
            return prefs;
        }

        private static TimeSpan GetFrequencyInterval(string? frequencia)
        {
            return (frequencia ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "imediata" => TimeSpan.Zero,
                "semanal" => TimeSpan.FromDays(7),
                _ => TimeSpan.FromDays(1), // default: Diaria
            };
        }

        private async Task<bool> CanGenerateNovaEstreiaAsync(string userId, PreferenciasNotificacao prefs, DateTime nowUtc)
        {
            if (!prefs.NovaEstreiaAtiva) return false;

            var interval = GetFrequencyInterval(prefs.NovaEstreiaFrequencia);
            if (interval == TimeSpan.Zero) return true; // Imediata

            var lastCreatedAt = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia)
                .MaxAsync(n => (DateTime?)n.CriadaEm);

            if (!lastCreatedAt.HasValue) return true;
            return nowUtc - lastCreatedAt.Value >= interval;
        }

        private static bool IsUpcoming(Filme f, DateTime nowUtc, DateTime endUtc, int maxAnoAhead)
        {
            if (f == null) return false;

            if (f.ReleaseDate.HasValue)
            {
                var rd = f.ReleaseDate.Value;
                // Comparar por data (date-only) para não “sumir” no mesmo dia por causa de timezone/horas.
                var todayUtc = nowUtc.Date;
                var endDateUtc = endUtc.Date;
                var rdDate = rd.Date;
                return rdDate >= todayUtc && rdDate <= endDateUtc;
            }

            if (f.Ano.HasValue)
            {
                var y = f.Ano.Value;
                var nowYear = nowUtc.Year;
                // Se só temos ano (sem releaseDate), consideramos também o ano corrente como “potencialmente futuro”.
                // Caso contrário, para o 1º trimestre do ano (ex.: jan->mar), removemos filmes com Ano==anoAtual.
                return y >= nowYear && y <= nowYear + maxAnoAhead;
            }

            return false;
        }

        private static DateTime SortKey(Filme f, DateTime nowUtc, int maxAnoAhead)
        {
            if (f.ReleaseDate.HasValue) return f.ReleaseDate.Value;
            if (f.Ano.HasValue) return new DateTime(f.Ano.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return new DateTime(nowUtc.Year + maxAnoAhead, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// Nomes em PT da tabela <c>Generos</c> → id oficial de género no TMDB (movie).
        private static readonly Dictionary<string, int> GeneroNomeParaTmdbId = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ação"] = 28,
            ["Aventura"] = 12,
            ["Comédia"] = 35,
            ["Crime"] = 80,
            ["Drama"] = 18,
            ["Fantasia"] = 14,
            ["Ficção Científica"] = 878,
            ["Horror"] = 27,
            ["Mistério"] = 9648,
            ["Romance"] = 10749,
            ["Thriller"] = 53,
            ["Animação"] = 16,
            ["Documentário"] = 99,
            ["Família"] = 10751,
            ["Guerra"] = 10752,
            ["Western"] = 37,
        };

        private bool GenreMatches(Filme f, List<string> favoriteGenres)
        {
            if (favoriteGenres == null || favoriteGenres.Count == 0) return false;
            if (f == null) return false;

            var generoText = f.Genero ?? string.Empty;

            static string Norm(string? s)
            {
                if (s == null) return string.Empty;
                var trimmed = s.Trim();
                var formD = trimmed.Normalize(System.Text.NormalizationForm.FormD);
                var sb = new System.Text.StringBuilder(formD.Length);
                foreach (var ch in formD)
                {
                    var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                    if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                        sb.Append(ch);
                }
                return sb.ToString().ToLowerInvariant();
            }

            var genrePtToEn = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Ação"] = ["Action"],
                ["Aventura"] = ["Adventure"],
                ["Comédia"] = ["Comedy"],
                ["Crime"] = ["Crime"],
                ["Drama"] = ["Drama"],
                ["Fantasia"] = ["Fantasy"],
                ["Ficção Científica"] = ["Science Fiction", "Sci-Fi"],
                ["Horror"] = ["Horror"],
                ["Mistério"] = ["Mystery"],
                ["Romance"] = ["Romance"],
                ["Thriller"] = ["Thriller"],
                ["Animação"] = ["Animation", "Animated"],
                ["Documentário"] = ["Documentary"],
                ["Família"] = ["Family"],
                ["Guerra"] = ["War"],
                ["Western"] = ["Western"],
            };

            var genrePtNormToEnNorms = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in genrePtToEn)
            {
                var ptNorm = Norm(kvp.Key);
                var enNorms = kvp.Value.Select(Norm).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                if (!string.IsNullOrWhiteSpace(ptNorm) && enNorms.Count > 0)
                    genrePtNormToEnNorms[ptNorm] = enNorms;
            }

            var favoriteTmdbIds = new HashSet<int>();
            foreach (var g in favoriteGenres)
            {
                if (string.IsNullOrWhiteSpace(g)) continue;
                if (GeneroNomeParaTmdbId.TryGetValue(g.Trim(), out var tid))
                    favoriteTmdbIds.Add(tid);
            }

            if (f.TmdbGenreIds is { Count: > 0 } filmIds && favoriteTmdbIds.Count > 0)
            {
                foreach (var id in filmIds)
                {
                    if (favoriteTmdbIds.Contains(id))
                        return true;
                }
            }

            var tokens = generoText
                .Split(new[] { ',', '/', '|', '&' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Norm)
                .Where(t => t.Length > 0)
                .ToHashSet();

            foreach (var g in favoriteGenres)
            {
                if (string.IsNullOrWhiteSpace(g)) continue;

                var gNorm = Norm(g);
                if (tokens.Contains(gNorm))
                    return true;

                if (genrePtNormToEnNorms.TryGetValue(gNorm, out var enNorms))
                {
                    foreach (var enNorm in enNorms)
                    {
                        if (!string.IsNullOrEmpty(enNorm) && tokens.Contains(enNorm))
                            return true;
                    }
                }
            }

            return false;
        }

        /// Retorna a lista de filmes para “NovaEstreia” já filtrada por géneros favoritos e histórico.
        /// Também cria notificações em `Notificacoes` para os filmes elegíveis.
        [HttpGet("nova-estreia")]
        public async Task<ActionResult<List<Filme>>> GetNovaEstreia(
            [FromQuery] int limit = 5,
            [FromQuery] int windowDays = 60,
            [FromQuery] int maxAnoAhead = 2)
        {
            if (limit < 1) limit = 5;
            if (limit > 10) limit = 10;
            if (windowDays < 1) windowDays = 60;
            if (maxAnoAhead < 0) maxAnoAhead = 0;

            var userId = GetUserId();

            var nowUtc = DateTime.UtcNow;
            var endUtc = nowUtc.AddDays(windowDays);

            // Sem user autenticado: não personaliza nem cria notificações.
            if (string.IsNullOrWhiteSpace(userId))
            {
                var genericCandidates = await _context.Filmes
                    .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                                || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                    .Take(250)
                    .ToListAsync();

                var sorted = genericCandidates
                    .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                    .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                    .Take(limit)
                    .ToList();

                return Ok(sorted);
            }

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            if (!prefs.NovaEstreiaAtiva)
            {
                return Ok(new List<Filme>());
            }

            var favoriteGenres = await _context.UtilizadorGeneros
                .Where(ug => ug.UtilizadorId == userId)
                .Select(ug => ug.Genero.Nome)
                .ToListAsync();

            favoriteGenres = favoriteGenres
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Se o utilizador não tem géneros favoritos, não geramos notificações “personalizadas”.
            if (favoriteGenres.Count == 0)
            {
                return Ok(new List<Filme>());
            }

            // Se o utilizador selecionou "quase todos" os géneros, assumimos que não deve haver filtragem apertada.
            var totalGenres = await _context.Generos.CountAsync();
            // Permitimos 1 género "em falta" para não bloquear a lista por detalhes de persistência.
            var applyGenreFilter = totalGenres > 0 && favoriteGenres.Count < Math.Max(1, totalGenres - 1);

            var watchedIds = await _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync();

            var watchedSet = watchedIds.ToHashSet();

            var candidates = await _context.Filmes
                .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                            || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                .Take(400)
                .ToListAsync();

            // Fallback: se a BD praticamente não tem “estreias” dentro da janela,
            // vamos buscar ao TMDB para conseguir ter notificações suficientes.
            var fallbackThreshold = Math.Max(10, limit * 2);
            if (candidates.Count < fallbackThreshold)
            {
                var upcomingFromTmdb = await _movieService.GetUpcomingMoviesAsync(page: 1, count: 60);
                if (upcomingFromTmdb != null && upcomingFromTmdb.Any())
                {
                    var tmdbIds = upcomingFromTmdb
                        .Select(x => x.TmdbId)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var existingTmdbIds = await _context.Filmes
                        .Where(f => tmdbIds.Contains(f.TmdbId))
                        .Select(f => f.TmdbId)
                        .ToListAsync();

                    var existingTmdbSet = existingTmdbIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var m in upcomingFromTmdb)
                    {
                        if (string.IsNullOrWhiteSpace(m.TmdbId)) continue;
                        if (existingTmdbSet.Contains(m.TmdbId)) continue;
                        m.Id = 0;
                        _context.Filmes.Add(m);
                    }

                    await _context.SaveChangesAsync();
                }

                // Recalcular candidatos após inserir novos filmes.
                candidates = await _context.Filmes
                    .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                                || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                    .Take(400)
                    .ToListAsync();
            }

            var eligible = candidates
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                .Where(f => !applyGenreFilter || GenreMatches(f, favoriteGenres))
                .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                .ToList();

            // Cria notificações NovaEstreia para os elegíveis (idempotente pelo índice único).
            if (await CanGenerateNovaEstreiaAsync(userId, prefs, nowUtc))
            {
                var desiredCreateCount = Math.Min(Math.Max(limit * 20, 50), 200);
                var poolToConsider = eligible.Take(desiredCreateCount).ToList();
                var poolIds = poolToConsider.Select(f => f.Id).ToList();

                var alreadyExistingIds = await _context.Notificacoes
                    .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.FilmeId != null && poolIds.Contains(n.FilmeId.Value))
                    .Select(n => n.FilmeId!.Value)
                    .ToListAsync();

                var existingNotifSet = alreadyExistingIds.ToHashSet();
                foreach (var f in poolToConsider)
                {
                    if (existingNotifSet.Contains(f.Id)) continue;

                    _context.Notificacoes.Add(new Notificacao
                    {
                        UtilizadorId = userId,
                        FilmeId = f.Id,
                        Tipo = TipoNovaEstreia,
                        CriadaEm = nowUtc
                    });
                }

                await _context.SaveChangesAsync();
            }

            // Devolve só notificações não lidas, revalidando elegibilidade.
            var unread = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm == null)
                .Include(n => n.Filme)
                .ToListAsync();

            var unreadMovies = unread
                .Select(n => n.Filme)
                .Where(f => f != null)
                .Cast<Filme>()
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                // Respeita o mesmo critério de filtragem que usamos na criação
                .Where(f => !applyGenreFilter || GenreMatches(f, favoriteGenres))
                .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                .Take(limit)
                .ToList();

            return Ok(unreadMovies);
        }

        public class NovaEstreiaFeedDto
        {
            public List<Filme> Unread { get; set; } = new();
            public List<Filme> Read { get; set; } = new();
        }

        public class LidosTmdbIdsDto
        {
            public List<int> LidosTmdbIds { get; set; } = new();
        }

        /// Devolve quais TMDB ids (entre os pedidos) têm notificação NovaEstreia marcada como lida.
        /// Usado pela lista “upcoming” TMDB na UI (filmes podem ainda não ter linha de notificação).
        [HttpGet("nova-estreia/lidos-tmdb-ids")]
        public async Task<ActionResult<LidosTmdbIdsDto>> GetLidosTmdbIds([FromQuery] string? ids = null)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var tmdbIds = ParseTmdbIdList(ids, max: 120);
            if (tmdbIds.Count == 0)
                return Ok(new LidosTmdbIdsDto());

            var tmdbStrSet = tmdbIds.Select(x => x.ToString()).ToHashSet(StringComparer.Ordinal);
            var pairs = await _context.Filmes
                .AsNoTracking()
                .Where(f => f.TmdbId != null && tmdbStrSet.Contains(f.TmdbId))
                .Select(f => new { f.Id, f.TmdbId })
                .ToListAsync();

            if (pairs.Count == 0)
                return Ok(new LidosTmdbIdsDto());

            var filmeIds = pairs.Select(p => p.Id).ToList();
            var readFilmeIds = await _context.Notificacoes
                .AsNoTracking()
                .Where(n =>
                    n.UtilizadorId == userId &&
                    n.Tipo == TipoNovaEstreia &&
                    n.LidaEm != null &&
                    n.FilmeId != null &&
                    filmeIds.Contains(n.FilmeId.Value))
                .Select(n => n.FilmeId!.Value)
                .ToListAsync();

            var readSet = readFilmeIds.ToHashSet();
            var lidos = new List<int>();
            foreach (var p in pairs)
            {
                if (!readSet.Contains(p.Id)) continue;
                if (int.TryParse(p.TmdbId, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var tid))
                    lidos.Add(tid);
            }

            return Ok(new LidosTmdbIdsDto { LidosTmdbIds = lidos });
        }

        private static List<int> ParseTmdbIdList(string? csv, int max)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var r = new List<int>();
            foreach (var p in parts)
            {
                if (r.Count >= max) break;
                if (int.TryParse(p, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var x) && x > 0)
                    r.Add(x);
            }
            return r.Distinct().ToList();
        }

        /// Lista “Próximas estreias” para a campainha (TMDB): opcionalmente filtrada pelos géneros favoritos do utilizador e exclui filmes já vistos.
        [HttpGet("proximas-estreias")]
        public async Task<ActionResult<List<Filme>>> GetProximasEstreiasPersonalizadas(
            [FromQuery] int page = 1,
            [FromQuery] int count = 40,
            [FromQuery] int windowDays = 180,
            [FromQuery] int maxAnoAhead = 5,
            [FromQuery] bool filtrarPorGeneros = true)
        {
            if (page < 1) page = 1;
            if (count < 1) count = 40;
            count = Math.Min(count, 40);
            if (windowDays < 1) windowDays = 180;
            if (maxAnoAhead < 0) maxAnoAhead = 0;

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            if (!prefs.NovaEstreiaAtiva)
                return Ok(new List<Filme>());

            var nowUtc = DateTime.UtcNow;
            var endUtc = nowUtc.AddDays(windowDays);
            var todayUtc = nowUtc.Date;

            var rawList = await _movieService.GetUpcomingMoviesAccumulatedAsync(page, count, todayUtc, maxPagesToScan: 12);
            var films = rawList ?? new List<Filme>();

            var favoriteGenres = await _context.UtilizadorGeneros
                .Where(ug => ug.UtilizadorId == userId)
                .Select(ug => ug.Genero.Nome)
                .ToListAsync();

            favoriteGenres = favoriteGenres
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Com filtrarPorGeneros=true, aplicamos sempre que existam géneros favoritos na BD.
            // (A antiga regra "quase todos os géneros = sem filtro" fazia "Os meus géneros" igual a "Todos".)
            var applyGenreFilter = filtrarPorGeneros && favoriteGenres.Count > 0;

            var watchedIds = await _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync();
            var watchedSet = watchedIds.ToHashSet();

            var tmdbNeedingDbId = films
                .Where(f => f.Id <= 0 && !string.IsNullOrWhiteSpace(f.TmdbId))
                .Select(f => f.TmdbId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tmdbToDbId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (tmdbNeedingDbId.Count > 0)
            {
                var rows = await _context.Filmes
                    .AsNoTracking()
                    .Where(f => f.TmdbId != null && tmdbNeedingDbId.Contains(f.TmdbId))
                    .Select(f => new { TmdbId = f.TmdbId!, f.Id })
                    .ToListAsync();
                foreach (var r in rows)
                {
                    if (!tmdbToDbId.ContainsKey(r.TmdbId))
                        tmdbToDbId[r.TmdbId] = r.Id;
                }
            }

            bool IsWatched(Filme f)
            {
                if (f.Id > 0) return watchedSet.Contains(f.Id);
                if (!string.IsNullOrWhiteSpace(f.TmdbId) && tmdbToDbId.TryGetValue(f.TmdbId, out var dbId))
                    return watchedSet.Contains(dbId);
                return false;
            }

            var filtered = films
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !IsWatched(f))
                .Where(f => !applyGenreFilter || GenreMatches(f, favoriteGenres))
                .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                .ToList();

            return Ok(filtered);
        }

        /// Feed para a UI: devolve não lidas + lidas (últimas N) para o menu de notificações.
        [HttpGet("nova-estreia/feed")]
        public async Task<ActionResult<NovaEstreiaFeedDto>> GetNovaEstreiaFeed(
            [FromQuery] int unreadLimit = 5,
            [FromQuery] int readLimit = 5,
            [FromQuery] int windowDays = 180,
            [FromQuery] int maxAnoAhead = 5)
        {
            if (unreadLimit < 0) unreadLimit = 0;
            if (unreadLimit > 10) unreadLimit = 10;
            if (readLimit < 0) readLimit = 0;
            if (readLimit > 10) readLimit = 10;
            if (windowDays < 1) windowDays = 180;
            if (maxAnoAhead < 0) maxAnoAhead = 0;

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            if (!prefs.NovaEstreiaAtiva)
            {
                return Ok(new NovaEstreiaFeedDto());
            }

            var nowUtc = DateTime.UtcNow;
            var endUtc = nowUtc.AddDays(windowDays);

            var favoriteGenres = await _context.UtilizadorGeneros
                .Where(ug => ug.UtilizadorId == userId)
                .Select(ug => ug.Genero.Nome)
                .ToListAsync();

            favoriteGenres = favoriteGenres
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var totalGenres = await _context.Generos.CountAsync();
            var applyGenreFilter = totalGenres > 0 && favoriteGenres.Count < Math.Max(1, totalGenres - 1);

            var watchedIds = await _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync();
            var watchedSet = watchedIds.ToHashSet();

            // Garantir que há pelo menos notificações criadas para candidatos dentro da janela
            _ = await GetNovaEstreia(unreadLimit == 0 ? 1 : unreadLimit, windowDays, maxAnoAhead);

            var unreadNotifs = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm == null)
                .Include(n => n.Filme)
                .ToListAsync();

            var unreadMovies = unreadNotifs
                .Select(n => n.Filme)
                .Where(f => f != null)
                .Cast<Filme>()
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                .Where(f => !applyGenreFilter || GenreMatches(f, favoriteGenres))
                .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                .Take(unreadLimit)
                .ToList();

            var readNotifs = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm != null)
                .Include(n => n.Filme)
                .OrderByDescending(n => n.LidaEm)
                .Take(40)
                .ToListAsync();

            var readMovies = readNotifs
                .Select(n => n.Filme)
                .Where(f => f != null)
                .Cast<Filme>()
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                .Where(f => !applyGenreFilter || GenreMatches(f, favoriteGenres))
                .Take(readLimit)
                .ToList();

            return Ok(new NovaEstreiaFeedDto
            {
                Unread = unreadMovies,
                Read = readMovies
            });
        }

        /// Endpoint de diagnóstico para perceber porque a lista devolvida fica pequena.
        [HttpGet("nova-estreia/debug")]
        public async Task<ActionResult<NovaEstreiaDebugDto>> DebugNovaEstreia(
            [FromQuery] int limit = 5,
            [FromQuery] int windowDays = 180,
            [FromQuery] int maxAnoAhead = 5)
        {
            if (limit < 1) limit = 5;
            if (limit > 10) limit = 10;
            if (windowDays < 1) windowDays = 180;
            if (maxAnoAhead < 0) maxAnoAhead = 0;

            var nowUtc = DateTime.UtcNow;
            var endUtc = nowUtc.AddDays(windowDays);
            var userId = GetUserId();

            var dto = new NovaEstreiaDebugDto
            {
                UserId = userId,
                Limit = limit,
                WindowDays = windowDays,
                MaxAnoAhead = maxAnoAhead,
            };

            // Sem auth => "genérico"
            if (string.IsNullOrWhiteSpace(userId))
            {
                var genericCandidates = await _context.Filmes
                    .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                                || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                    .Take(250)
                    .ToListAsync();

                dto.CandidatesCount = genericCandidates.Count;
                dto.EligibleCount = genericCandidates.Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead)).Count();
                return Ok(dto);
            }

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            if (!prefs.NovaEstreiaAtiva)
            {
                dto.CandidatesCount = 0;
                dto.EligibleCount = 0;
                dto.UnreadNotificationsCount = 0;
                dto.ReadNotificationsCount = 0;
                return Ok(dto);
            }

            var favoriteGenres = await _context.UtilizadorGeneros
                .Where(ug => ug.UtilizadorId == userId)
                .Select(ug => ug.Genero.Nome)
                .ToListAsync();

            favoriteGenres = favoriteGenres
                .Select(x => x?.Trim() ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            dto.FavoriteGenresCount = favoriteGenres.Count;
            dto.TotalGenres = await _context.Generos.CountAsync();
            dto.ApplyGenreFilter = dto.TotalGenres.HasValue && favoriteGenres.Count < Math.Max(1, dto.TotalGenres.Value - 1);

            var watchedIds = await _context.UserMovies
                .Where(um => um.UtilizadorId == userId && um.JaViu)
                .Select(um => um.FilmeId)
                .ToListAsync();

            var watchedSet = watchedIds.ToHashSet();
            dto.WatchedCount = watchedSet.Count;

            var candidates = await _context.Filmes
                .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                            || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                .Take(400)
                .ToListAsync();

            dto.CandidatesCount = candidates.Count;

            // Fallback: se a BD praticamente não tem “estreias” dentro da janela,
            // buscamos ao TMDB e inserimos na BD para permitir mais candidatos.
            var fallbackThreshold = Math.Max(10, limit * 2);
            if (candidates.Count < fallbackThreshold)
            {
                var upcomingFromTmdb = await _movieService.GetUpcomingMoviesAsync(page: 1, count: 60);
                if (upcomingFromTmdb != null && upcomingFromTmdb.Any())
                {
                    var tmdbIds = upcomingFromTmdb
                        .Select(x => x.TmdbId)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var existingTmdbIds = await _context.Filmes
                        .Where(f => tmdbIds.Contains(f.TmdbId))
                        .Select(f => f.TmdbId)
                        .ToListAsync();

                    var existingTmdbSet = existingTmdbIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var m in upcomingFromTmdb)
                    {
                        if (string.IsNullOrWhiteSpace(m.TmdbId)) continue;
                        if (existingTmdbSet.Contains(m.TmdbId)) continue;
                        m.Id = 0;
                        _context.Filmes.Add(m);
                    }

                    await _context.SaveChangesAsync();
                }

                candidates = await _context.Filmes
                    .Where(f => (f.ReleaseDate.HasValue && f.ReleaseDate.Value > nowUtc && f.ReleaseDate.Value <= endUtc)
                                || (f.ReleaseDate == null && f.Ano.HasValue && f.Ano.Value >= nowUtc.Year && f.Ano.Value <= nowUtc.Year + maxAnoAhead))
                    .Take(400)
                    .ToListAsync();

                dto.CandidatesCount = candidates.Count;
            }

            var eligible = candidates
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                .Where(f => !dto.ApplyGenreFilter || GenreMatches(f, favoriteGenres))
                .ToList();

            dto.EligibleCount = eligible.Count;

            dto.DesiredCreateCount = Math.Min(Math.Max(limit * 20, 50), 200);
            var poolToConsider = eligible.Take(dto.DesiredCreateCount).ToList();
            dto.PoolToConsiderCount = poolToConsider.Count;

            if (await CanGenerateNovaEstreiaAsync(userId, prefs, nowUtc))
            {
                var poolIds = poolToConsider.Select(f => f.Id).ToList();
                var alreadyExistingIds = await _context.Notificacoes
                    .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.FilmeId != null && poolIds.Contains(n.FilmeId.Value))
                    .Select(n => n.FilmeId!.Value)
                    .ToListAsync();

                var existingNotifSet = alreadyExistingIds.ToHashSet();
                foreach (var f in poolToConsider)
                {
                    if (existingNotifSet.Contains(f.Id)) continue;

                    _context.Notificacoes.Add(new Notificacao
                    {
                        UtilizadorId = userId,
                        FilmeId = f.Id,
                        Tipo = TipoNovaEstreia,
                        CriadaEm = nowUtc
                    });
                }

                await _context.SaveChangesAsync();
            }

            dto.UnreadNotificationsCount = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm == null)
                .CountAsync();

            // Preview dos filmes que de facto seriam devolvidos pelo endpoint principal
            // (isto explica quando o número de notificações “não lidas” diverge do que a UI mostra).
            var unread = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm == null)
                .Include(n => n.Filme)
                .ToListAsync();

            var unreadMovies = unread
                .Select(n => n.Filme)
                .Where(f => f != null)
                .Cast<Filme>()
                .Where(f => IsUpcoming(f, nowUtc, endUtc, maxAnoAhead))
                .Where(f => !watchedSet.Contains(f.Id))
                .Where(f => !dto.ApplyGenreFilter || GenreMatches(f, favoriteGenres))
                .OrderBy(f => SortKey(f, nowUtc, maxAnoAhead))
                .Take(limit)
                .ToList();

            dto.UnreadPreview = unreadMovies.Select(f => new FilmePreviewDto
            {
                FilmeId = f.Id,
                Titulo = f.Titulo,
                ReleaseDate = f.ReleaseDate?.ToString("yyyy-MM-dd"),
                Ano = f.Ano
            }).ToList();

            var read = await _context.Notificacoes
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoNovaEstreia && n.LidaEm != null)
                .Include(n => n.Filme)
                .OrderByDescending(n => n.LidaEm)
                .Take(10)
                .ToListAsync();

            dto.ReadNotificationsCount = read.Count;
            dto.ReadPreview = read
                .Select(n => n.Filme)
                .Where(f => f != null)
                .Cast<Filme>()
                .Select(f => new FilmePreviewDto
                {
                    FilmeId = f.Id,
                    Titulo = f.Titulo,
                    ReleaseDate = f.ReleaseDate?.ToString("yyyy-MM-dd"),
                    Ano = f.Ano
                })
                .ToList();

            return Ok(dto);
        }

        [HttpPut("nova-estreia/{filmeId:int}/lida")]
        public async Task<IActionResult> MarcarNovaEstreiaComoLida([FromRoute] int filmeId)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var nowUtc = DateTime.UtcNow;

            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.UtilizadorId == userId && n.FilmeId == filmeId && n.Tipo == TipoNovaEstreia);

            if (notif == null)
            {
                _context.Notificacoes.Add(new Notificacao
                {
                    UtilizadorId = userId,
                    FilmeId = filmeId,
                    Tipo = TipoNovaEstreia,
                    CriadaEm = nowUtc,
                    LidaEm = nowUtc
                });
            }
            else
            {
                notif.LidaEm = nowUtc;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }



        private static readonly JsonSerializerOptions ResumoJsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [HttpGet("preferencias-notificacao")]
        public async Task<ActionResult<PreferenciasNotificacaoDto>> GetPreferenciasNotificacao()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            return Ok(new PreferenciasNotificacaoDto
            {
                NovaEstreiaAtiva = prefs.NovaEstreiaAtiva,
                NovaEstreiaFrequencia = prefs.NovaEstreiaFrequencia,
                ResumoEstatisticasAtiva = prefs.ResumoEstatisticasAtiva,
                ReminderJogoAtiva = prefs.ReminderJogoAtiva,
                ResumoEstatisticasFrequencia = string.IsNullOrWhiteSpace(prefs.ResumoEstatisticasFrequencia)
                    ? "Semanal"
                    : prefs.ResumoEstatisticasFrequencia
            });
        }

        [HttpPut("preferencias-notificacao")]
        public async Task<IActionResult> PutPreferenciasNotificacao([FromBody] PreferenciasNotificacaoDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var freq = (dto.NovaEstreiaFrequencia ?? string.Empty).Trim();
            if (!FrequenciasPermitidas.Contains(freq, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Frequência inválida (novas estreias).",
                    allowed = FrequenciasPermitidas
                });
            }

            var freqResumoRaw = (dto.ResumoEstatisticasFrequencia ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(freqResumoRaw) &&
                !ResumoFrequenciasPermitidas.Contains(freqResumoRaw, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Frequência inválida (resumo de estatísticas).",
                    allowed = ResumoFrequenciasPermitidas
                });
            }

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            prefs.NovaEstreiaAtiva = dto.NovaEstreiaAtiva;
            prefs.NovaEstreiaFrequencia = freq;
            prefs.ReminderJogoAtiva = dto.ReminderJogoAtiva;
            if (dto.ResumoEstatisticasAtiva.HasValue)
                prefs.ResumoEstatisticasAtiva = dto.ResumoEstatisticasAtiva.Value;
            if (!string.IsNullOrEmpty(freqResumoRaw))
                prefs.ResumoEstatisticasFrequencia = freqResumoRaw;
            prefs.AtualizadaEm = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// Feed FR70: resumos periódicos de estatísticas (lidas / não lidas).
        [HttpGet("resumo-estatisticas/feed")]
        public async Task<ActionResult<ResumoEstatisticasFeedDto>> GetResumoEstatisticasFeed(
            [FromQuery] int unreadLimit = 5,
            [FromQuery] int readLimit = 5)
        {
            if (unreadLimit < 0) unreadLimit = 0;
            if (unreadLimit > 20) unreadLimit = 20;
            if (readLimit < 0) readLimit = 0;
            if (readLimit > 20) readLimit = 20;

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var prefs = await GetOrCreatePreferenciasNotificacaoAsync(userId);
            if (!prefs.ResumoEstatisticasAtiva)
                return Ok(new ResumoEstatisticasFeedDto());

            static ResumoEstatisticasCorpoDto? ParseCorpo(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                try
                {
                    return JsonSerializer.Deserialize<ResumoEstatisticasCorpoDto>(raw, ResumoJsonOpts);
                }
                catch
                {
                    return null;
                }
            }

            var unread = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoResumoEstatisticas && n.LidaEm == null)
                .OrderByDescending(n => n.CriadaEm)
                .Take(unreadLimit)
                .ToListAsync();

            var read = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoResumoEstatisticas && n.LidaEm != null)
                .OrderByDescending(n => n.LidaEm)
                .Take(readLimit)
                .ToListAsync();

            return Ok(new ResumoEstatisticasFeedDto
            {
                Unread = unread.Select(n => new ResumoEstatisticasFeedItemDto
                {
                    Id = n.Id,
                    CriadaEm = n.CriadaEm,
                    LidaEm = n.LidaEm,
                    Corpo = ParseCorpo(n.Corpo)
                }).ToList(),
                Read = read.Select(n => new ResumoEstatisticasFeedItemDto
                {
                    Id = n.Id,
                    CriadaEm = n.CriadaEm,
                    LidaEm = n.LidaEm,
                    Corpo = ParseCorpo(n.Corpo)
                }).ToList()
            });
        }

        [HttpPut("resumo-estatisticas/{id:int}/lida")]
        public async Task<IActionResult> MarcarResumoEstatisticasComoLida([FromRoute] int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var nowUtc = DateTime.UtcNow;
            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilizadorId == userId && n.Tipo == TipoResumoEstatisticas);

            if (notif == null)
                return NotFound();

            notif.LidaEm = nowUtc;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// Feed de notificações de comunidade (novas publicações).
        [HttpGet("comunidade/feed")]
        public async Task<ActionResult<NotificacaoComunidadeFeedDto>> GetNotificacoesComunidadeFeed(
            [FromQuery] int unreadLimit = 20,
            [FromQuery] int readLimit = 10)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (unreadLimit < 0) unreadLimit = 0;
            if (unreadLimit > 50) unreadLimit = 50;
            if (readLimit < 0) readLimit = 0;
            if (readLimit > 50) readLimit = 50;

            var unread = await _context.NotificacoesComunidade
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.LidaEm == null)
                .OrderByDescending(n => n.CriadaEm)
                .Take(unreadLimit)
                .Select(n => new NotificacaoComunidadeItemDto
                {
                    Id = n.Id,
                    ComunidadeId = n.ComunidadeId,
                    ComunidadeNome = _context.Comunidades
                        .Where(c => c.Id == n.ComunidadeId)
                        .Select(c => c.Nome)
                        .FirstOrDefault() ?? "",
                    PostId = n.PostId,
                    CriadaEm = n.CriadaEm,
                    LidaEm = n.LidaEm
                })
                .ToListAsync();

            var read = await _context.NotificacoesComunidade
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.LidaEm != null)
                .OrderByDescending(n => n.LidaEm)
                .Take(readLimit)
                .Select(n => new NotificacaoComunidadeItemDto
                {
                    Id = n.Id,
                    ComunidadeId = n.ComunidadeId,
                    ComunidadeNome = _context.Comunidades
                        .Where(c => c.Id == n.ComunidadeId)
                        .Select(c => c.Nome)
                        .FirstOrDefault() ?? "",
                    PostId = n.PostId,
                    CriadaEm = n.CriadaEm,
                    LidaEm = n.LidaEm
                })
                .ToListAsync();

            return Ok(new NotificacaoComunidadeFeedDto { Unread = unread, Read = read });
        }

        /// Contagem de notificações de comunidade não lidas.
        [HttpGet("comunidade/unread-count")]
        public async Task<ActionResult<int>> GetNotificacoesComunidadeUnreadCount()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var count = await _context.NotificacoesComunidade
                .CountAsync(n => n.UtilizadorId == userId && n.LidaEm == null);

            return Ok(count);
        }

        /// Marcar uma notificação de comunidade como lida.
        [HttpPut("comunidade/{id:int}/lida")]
        public async Task<IActionResult> MarcarNotificacaoComunidadeComoLida([FromRoute] int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var notif = await _context.NotificacoesComunidade
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilizadorId == userId);

            if (notif == null)
                return NotFound();

            notif.LidaEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// Marcar todas as notificações de comunidade como lidas.
        [HttpPut("comunidade/marcar-todas-lidas")]
        public async Task<IActionResult> MarcarTodasNotificacoesComunidadeComoLidas()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var nowUtc = DateTime.UtcNow;
            var unread = await _context.NotificacoesComunidade
                .Where(n => n.UtilizadorId == userId && n.LidaEm == null)
                .ToListAsync();

            foreach (var n in unread)
                n.LidaEm = nowUtc;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("reminder-jogo/feed")]
        public async Task<IActionResult> GetReminderJogoFeed()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var notifs = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoReminderJogo && n.LidaEm == null)
                .OrderByDescending(n => n.CriadaEm)
                .Take(5)
                .Select(n => new { n.Id, n.Corpo, n.CriadaEm })
                .ToListAsync();

            return Ok(notifs);
        }

        [HttpPut("reminder-jogo/{id:int}/lida")]
        public async Task<IActionResult> MarcarReminderJogoComoLida([FromRoute] int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilizadorId == userId && n.Tipo == TipoReminderJogo);

            if (notif == null) return NotFound();

            notif.LidaEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }


        [HttpGet("filme-disponivel/feed")]
        public async Task<IActionResult> GetFilmeDisponivelFeed()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var items = await _context.Notificacoes
                .AsNoTracking()
                .Where(n => n.UtilizadorId == userId && n.Tipo == TipoFilmeDisponivel && n.LidaEm == null)
                .OrderByDescending(n => n.CriadaEm)
                .Take(30)
                .Select(n => new FilmeDisponivelFeedItemDto
                {
                    Id = n.Id,
                    FilmeId = n.FilmeId,
                    Titulo = n.Filme != null ? n.Filme.Titulo : null,
                    Corpo = n.Corpo ?? "",
                    CriadaEm = n.CriadaEm
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPut("filme-disponivel/{id:int}/lida")]
        public async Task<IActionResult> MarcarFilmeDisponivelComoLida([FromRoute] int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notif = await _context.Notificacoes
                .FirstOrDefaultAsync(n => n.Id == id && n.UtilizadorId == userId && n.Tipo == TipoFilmeDisponivel);

            if (notif == null) return NotFound();

            notif.LidaEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }


        public class FilmeDisponivelFeedItemDto
        {
            public int Id { get; set; }
            public int? FilmeId { get; set; }
            public string? Titulo { get; set; }
            public string Corpo { get; set; } = "";
            public DateTime CriadaEm { get; set; }
        }

        public class NovaEstreiaDebugDto
        {
            public string? UserId { get; set; }
            public int Limit { get; set; }
            public int WindowDays { get; set; }
            public int MaxAnoAhead { get; set; }

            public int? TotalGenres { get; set; }
            public int FavoriteGenresCount { get; set; }
            public bool ApplyGenreFilter { get; set; }

            public int WatchedCount { get; set; }
            public int CandidatesCount { get; set; }
            public int EligibleCount { get; set; }
            public int DesiredCreateCount { get; set; }
            public int PoolToConsiderCount { get; set; }
            public int UnreadNotificationsCount { get; set; }

            public List<FilmePreviewDto> UnreadPreview { get; set; } = new();

            public int ReadNotificationsCount { get; set; }
            public List<FilmePreviewDto> ReadPreview { get; set; } = new();
        }

        public class FilmePreviewDto
        {
            public int FilmeId { get; set; }
            public string? Titulo { get; set; }
            public string? ReleaseDate { get; set; }
            public int? Ano { get; set; }
        }

        public class PreferenciasNotificacaoDto
        {
            public bool NovaEstreiaAtiva { get; set; } = true;
            public string NovaEstreiaFrequencia { get; set; } = "Diaria";
            /// Null se o cliente não enviou o campo (mantém valor na BD).
            public bool? ResumoEstatisticasAtiva { get; set; }
            /// Null ou vazio: mantém frequência na BD.
            public string? ResumoEstatisticasFrequencia { get; set; }

            public bool ReminderJogoAtiva { get; set; } = true;

            public bool FilmeDisponivelAtiva { get; set; } = true;
        }

        public class ResumoEstatisticasFeedItemDto
        {
            public int Id { get; set; }
            public DateTime CriadaEm { get; set; }
            public DateTime? LidaEm { get; set; }
            public ResumoEstatisticasCorpoDto? Corpo { get; set; }
        }

        public class ResumoEstatisticasFeedDto
        {
            public List<ResumoEstatisticasFeedItemDto> Unread { get; set; } = new();
            public List<ResumoEstatisticasFeedItemDto> Read { get; set; } = new();
        }

        public class NotificacaoComunidadeItemDto
        {
            public int Id { get; set; }
            public int ComunidadeId { get; set; }
            public string ComunidadeNome { get; set; } = "";
            public int PostId { get; set; }
            public DateTime CriadaEm { get; set; }
            public DateTime? LidaEm { get; set; }
        }

        public class NotificacaoComunidadeFeedDto
        {
            public List<NotificacaoComunidadeItemDto> Unread { get; set; } = new();
            public List<NotificacaoComunidadeItemDto> Read { get; set; } = new();
        }
    }
}