using System.Security.Claims;
using FilmAholic.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace FilmAholic.Server.Middleware;

/// <summary>
/// Após autenticação por cookie: se o utilizador foi eliminado ou está em lockout (bloqueado pelo admin),
/// termina a sessão e devolve 403 com <c>sessaoTerminadaMotivo</c> para o cliente mostrar mensagem.
/// </summary>
public sealed class AccountStatusMiddleware
{
    private readonly RequestDelegate _next;

    public AccountStatusMiddleware(RequestDelegate next) => _next = next;

    private static bool IsAutenticacaoSessaoPath(PathString path)
    {
        var p = path.Value?.TrimEnd('/') ?? "";
        return p.EndsWith("/api/autenticacao/sessao", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ler o próprio perfil (GET api/Profile/{id}) mesmo em lockout, para o cliente mostrar "conta bloqueada".</summary>
    private static bool IsGetOwnProfilePath(HttpContext context, string currentUserId)
    {
        if (!HttpMethods.IsGet(context.Request.Method)) return false;
        if (string.IsNullOrEmpty(currentUserId)) return false;
        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 3) return false;
        if (!parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)) return false;
        if (!parts[1].Equals("Profile", StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(parts[2], currentUserId, StringComparison.Ordinal);
    }

    public async Task InvokeAsync(HttpContext context, UserManager<Utilizador> userManager)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || IsAutenticacaoSessaoPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await _next(context);
            return;
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { sessaoTerminadaMotivo = "eliminada" });
            return;
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            if (IsGetOwnProfilePath(context, userId))
            {
                await _next(context);
                return;
            }

            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { sessaoTerminadaMotivo = "bloqueada" });
            return;
        }

        await _next(context);
    }
}
