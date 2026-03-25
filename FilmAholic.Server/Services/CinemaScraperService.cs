using FilmAholic.Server.DTOs;
using HtmlAgilityPack;
using System.Net;

namespace FilmAholic.Server.Services;

public class CinemaScraperService : ICinemaScraperService
{
    private readonly ILogger<CinemaScraperService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    // URLs fixas do Cinecartaz - representam toda a programação da cadeia
    private const string URL_NOS = "https://cinecartaz.publico.pt/cinema/zon-lusomundo-colombo-17538";
    private const string URL_CINEMA_CITY = "https://cinecartaz.publico.pt/cinema/cinemacity-campo-pequeno-169327";

    public CinemaScraperService(
        ILogger<CinemaScraperService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<CinemaMovieDto>> ScrapeAllAsync(CancellationToken ct = default)
    {
        var results = new List<CinemaMovieDto>();

        // Scrape NOS
        try
        {
            var nos = await ScrapeWithRetryAsync(() => ScrapeCinecartazAsync(URL_NOS, "Cinema NOS"), "Cinema NOS", ct);
            _logger.LogInformation("Cinema NOS: {Count} filmes", nos.Count);
            results.AddRange(nos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer scrape do Cinema NOS.");
        }

        // Scrape Cinema City
        try
        {
            var city = await ScrapeWithRetryAsync(() => ScrapeCinecartazAsync(URL_CINEMA_CITY, "Cinema City"), "Cinema City", ct);
            _logger.LogInformation("Cinema City: {Count} filmes", city.Count);
            results.AddRange(city);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer scrape do Cinema City.");
        }

        return results;
    }

    private async Task<List<CinemaMovieDto>> ScrapeWithRetryAsync(
        Func<Task<List<CinemaMovieDto>>> scraper,
        string name,
        CancellationToken ct,
        int maxAttempts = 3)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return await scraper();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = attempt * 2000;
                _logger.LogWarning("Tentativa {Attempt}/{Max} falhou para {Name}: {Msg}. A tentar em {Delay}ms.",
                    attempt, maxAttempts, name, ex.Message, delay);
                await Task.Delay(delay, ct);
            }
        }
        return new List<CinemaMovieDto>();
    }

    private async Task<List<CinemaMovieDto>> ScrapeCinecartazAsync(string url, string cinemaNome)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

        var html = await httpClient.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var movies = new List<CinemaMovieDto>();

        // No Cinecartaz, cada filme está numa <div> que contém:
        // - <img> com o poster
        // - <h3> com o título (ou às vezes <h2>)
        // - Parágrafo com "De:", "Com:", classificação
        // - Link "Saber mais"
        
        // Estratégia: encontrar todos os blocos que têm <img> + <h3> + link "Saber mais"
        // Isto corresponde a elementos que contêm estes 3 componentes juntos
        
        var allDivs = doc.DocumentNode.SelectNodes("//div[.//img and (.//h3 or .//h2) and .//a[contains(text(), 'Saber mais')]]");

        if (allDivs == null || !allDivs.Any())
        {
            _logger.LogWarning("Nenhum filme encontrado em {Url}", url);
            return movies;
        }

        foreach (var node in allDivs)
        {
            try
            {
                var movie = ExtractMovieFromNode(node, cinemaNome);
                if (movie != null && !string.IsNullOrWhiteSpace(movie.Titulo))
                {
                    movies.Add(movie);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro ao extrair filme: {Msg}", ex.Message);
            }
        }

        // Remove duplicados baseado no título
        return movies
            .GroupBy(m => m.Titulo.ToLower().Trim())
            .Select(g => g.First())
            .ToList();
    }

    private CinemaMovieDto? ExtractMovieFromNode(HtmlNode node, string cinemaNome)
    {
        // No Cinecartaz, cada filme está numa <div> com img + h3 título + p com "De:", "Com:", classificação, sessões, link "Saber mais"
        
        // Título - está num h3 dentro do bloco
        var tituloNode = node.SelectSingleNode(".//h3") ?? node.SelectSingleNode(".//h2");
        var titulo = tituloNode?.InnerText?.Trim();

        if (string.IsNullOrWhiteSpace(titulo))
            return null;

        titulo = WebUtility.HtmlDecode(titulo).Trim();

        // Poster - imagem no topo
        var posterNode = node.SelectSingleNode(".//img");
        var poster = posterNode?.GetAttributeValue("src", "") ?? "";
        
        if (!string.IsNullOrEmpty(poster))
        {
            if (poster.StartsWith("//"))
                poster = "https:" + poster;
            else if (poster.StartsWith("/"))
                poster = "https://imagens.publicocdn.com" + poster;
            // URLs do publicocdn já têm query params (?tp=KM), manter como está
        }

        // Classificação etária - procurar padrão M/XX
        var classificacao = "";
        var allText = node.InnerText;
        var classMatch = System.Text.RegularExpressions.Regex.Match(allText, @"M/\d+");
        if (classMatch.Success)
            classificacao = classMatch.Value;

        // Link "Saber mais"
        var linkNode = node.SelectSingleNode(".//a[contains(text(), 'Saber mais')]");
        var link = linkNode?.GetAttributeValue("href", "") ?? "";
        if (!string.IsNullOrEmpty(link) && link.StartsWith("/"))
            link = "https://cinecartaz.publico.pt" + link;

        // Género e duração - extrair da página de detalhes seria necessário,
        // mas por agora deixamos vazio (vamos buscar depois via TMDB se necessário)
        var genero = "";
        var duracao = "";

        return new CinemaMovieDto
        {
            Titulo = titulo,
            Poster = poster,
            Cinema = cinemaNome,
            Genero = genero,
            Duracao = duracao,
            Classificacao = classificacao,
            Link = link
        };
    }

    private string FormatDuration(string duracao)
    {
        if (string.IsNullOrWhiteSpace(duracao))
            return "";

        // Se já está formatado (ex: "2h 15min"), retorna direto
        if (duracao.Contains("h") && duracao.Contains("min"))
            return duracao;

        // Tenta extrair número de minutos
        var match = System.Text.RegularExpressions.Regex.Match(duracao, @"(\d+)");
        if (match.Success && int.TryParse(match.Value, out int mins))
        {
            var h = mins / 60;
            var m = mins % 60;
            return h > 0 ? $"{h}h {m}min" : $"{m}min";
        }

        return duracao;
    }
}