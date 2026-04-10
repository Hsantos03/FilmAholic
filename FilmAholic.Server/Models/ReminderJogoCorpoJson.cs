using System.Text.Json;
using System.Text.Json.Serialization;

namespace FilmAholic.Server.Models;

/// <summary>
/// Corpo da notificação ReminderJogo: JSON <c>{"t":"texto","i":0..9}</c> (variante = ícone por ordem das mensagens).
/// Notificações antigas: texto plano com emoji no fim — ver <see cref="Parse"/>.
/// </summary>
public static class ReminderJogoCorpoJson
{
    private sealed record Payload(
        [property: JsonPropertyName("t")] string T,
        [property: JsonPropertyName("i")] int I);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null
    };

    public static string Serialize(int variante, string texto) =>
        JsonSerializer.Serialize(new Payload(texto, variante), JsonOpts);

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
