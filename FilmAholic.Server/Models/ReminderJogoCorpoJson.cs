using System.Text.Json;
using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models;


/// <summary>
/// Representa o corpo da notificação ReminderJogo em formato JSON.
/// </summary>
public static class ReminderJogoCorpoJson
{
    /// <summary>
    /// Representa o payload da notificação ReminderJogo em formato JSON.
    /// </summary>
    private sealed record Payload(
        [property: JsonPropertyName("t")] string T,
        [property: JsonPropertyName("i")] int I);


    /// <summary>
    /// Opções de serialização JSON para o corpo da notificação ReminderJogo.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null
    };


    /// <summary>
    /// Serializa o corpo da notificação ReminderJogo em formato JSON.
    /// </summary>
    public static string Serialize(int variante, string texto) =>
        JsonSerializer.Serialize(new Payload(texto, variante), JsonOpts);


    /// <summary>
    /// Analisa o corpo da notificação ReminderJogo em formato JSON ou texto legado.
    /// </summary>
    public static (string Texto, int Variante) Parse(string? corpo)
    {
        if (string.IsNullOrWhiteSpace(corpo))
            return ("", 0);

        var s = corpo.Trim();
        if (s.StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                var p = JsonSerializer.Deserialize<Payload>(s, JsonOpts);
                if (p?.T != null)
                {
                    var v = p.I;
                    if (v < 0 || v > 9) v = 0;
                    return (p.T, v);
                }
            }
            catch
            {
                /* legacy */
            }
        }

        for (var idx = 0; idx < ReminderJogoMensagens.LegacyCorpoComEmoji.Length; idx++)
        {
            if (s == ReminderJogoMensagens.LegacyCorpoComEmoji[idx])
                return (ReminderJogoMensagens.TextosSemEmoji[idx], idx);
        }

        return (s, 0);
    }
}
