using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAholic.Server.Authentication;

/// <summary>
/// Classe auxiliar estática para lidar com falhas de correlação remotas durante o processo de autenticação OAuth (como o Google).
/// Quando o utilizador volta atrás no browser após OAuth, o GET a /signin-google pode repetir-se sem o cookie de correlação
/// (já consumido) — gera "Correlation failed".
/// </summary>
public static class OAuthRemoteFailureHelper
{
    /// <summary>
    /// Lida com a falha de autenticação redirecionando o utilizador de volta para a página de login do frontend com uma mensagem de erro compreensível.
    /// </summary>
    /// <param name="context"> O contexto da falha remota que contém a exceção e o contexto HTTP.</param>
    /// <returns>Uma tarefa concluída que assinala a finalização do processo.</returns>
    public static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var frontend = ResolveFrontendBaseUrl(context.HttpContext, config);
        var message = UserFacingMessage(context.Failure);
        context.Response.Redirect($"{frontend}/login?error={Uri.EscapeDataString(message)}");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Calcula e devolve o URL base do frontend para redirecionar, baseado na configuração ou host atual.
    /// </summary>
    /// <param name="http"> O contexto HTTP do pedido atual.</param>
    /// <param name="configuration"> A interface de configuração da aplicação para consultar definições do URL.</param>
    /// <returns>Uma string que representa o URL base do frontend.</returns>
    private static string ResolveFrontendBaseUrl(HttpContext http, IConfiguration configuration)
    {
        var host = http.Request.Host.Value ?? "";
        var isBackendLocalhost = host.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        var configured = configuration["FrontendBaseUrl"]
            ?? configuration["EmailSettings:AngularUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var url = configured.TrimEnd('/');
            if (!isBackendLocalhost && url.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                configured = null;
            else if (!string.IsNullOrEmpty(url))
                return url;
        }

        if (isBackendLocalhost)
            return "https://localhost:50905";

        var scheme = http.Request.Scheme;
        if (!http.Request.IsHttps && (http.Request.Headers["X-Forwarded-Proto"].ToString()?.ToLowerInvariant() ?? "") != "https")
            scheme = "https";
        return $"{scheme}://{host}";
    }

    /// <summary>
    /// Cria uma mensagem legível para o utilizador com base numa exceção recebida do fornecedor OAuth.
    /// </summary>
    /// <param name="failure"> Exceção recebida contendo as causas da falha.</param>
    /// <returns> Uma string com a mensagem traduzida e direcionada para o utilizador final.</returns>
    private static string UserFacingMessage(Exception? failure)
    {
        if (failure == null)
            return "Erro ao iniciar sessão com a conta externa. Tenta novamente.";

        var msg = failure.Message ?? "";
        if (msg.Contains("Correlation", StringComparison.OrdinalIgnoreCase))
            return "Este pedido de login já foi usado ou expirou (por exemplo, ao voltar atrás no browser). Volta à página de login e inicia sessão de novo.";

        return "Erro ao iniciar sessão com a conta externa. Tenta novamente.";
    }
}
