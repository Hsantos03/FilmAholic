using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FilmAholic.Server.Authentication;

/// <summary>
/// Quando o utilizador volta atrás no browser após OAuth, o GET a /signin-google pode repetir-se sem o cookie de correlação
/// (já consumido) — gera "Correlation failed". Tratamos aqui com redirect ao frontend em vez de página de exceção.
/// </summary>
public static class OAuthRemoteFailureHelper
{
    public static Task HandleRemoteFailure(RemoteFailureContext context)
    {
        context.HandleResponse();
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var frontend = ResolveFrontendBaseUrl(context.HttpContext, config);
        var message = UserFacingMessage(context.Failure);
        context.Response.Redirect($"{frontend}/login?error={Uri.EscapeDataString(message)}");
        return Task.CompletedTask;
    }

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
