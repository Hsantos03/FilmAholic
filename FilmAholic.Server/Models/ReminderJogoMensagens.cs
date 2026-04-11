namespace FilmAholic.Server.Models;

/// <summary>
/// Mensagens do reminder HoL (variante 0..9 = ordem do ícone no cliente).
/// </summary>
public static class ReminderJogoMensagens
{
    /// <summary>
    /// Textos das mensagens do reminder HoL sem emojis.
    /// </summary>
    public static readonly string[] TextosSemEmoji =
    [
        "Desafia-te e tenta chegar ao topo da Leaderboard!",
        "Há alguns dias que não jogas... O teu lugar no ranking está em risco!",
        "Os teus rivais estão a subir no ranking. Estás à espera de quê?",
        "Um novo desafio aguarda-te no Higher or Lower. Consegues bater o teu recorde?",
        "A Leaderboard não se conquista a descansar! Volta ao jogo!",
        "Sentes falta da adrenalina do Higher or Lower? Nós também sentimos a tua falta!",
        "Ainda tens o que é preciso? Prova isso no Higher or Lower!",
        "O jogo está à tua espera. Quanto tempo consegues aguentar?",
        "Hoje pode ser o dia em que bates o teu recorde! Vai lá tentar!",
        "A tua posição na Leaderboard depende de ti. Não a deixes escapar!"
    ];

    /// <summary>
    /// Textos das mensagens do reminder HoL com emojis.
    /// </summary>
    public static readonly string[] LegacyCorpoComEmoji =
    [
        "Desafia-te e tenta chegar ao topo da Leaderboard! 🎮",
        "Há alguns dias que não jogas... O teu lugar no ranking está em risco! 👀",
        "Os teus rivais estão a subir no ranking. Estás à espera de quê? 🏆",
        "Um novo desafio aguarda-te no Higher or Lower. Consegues bater o teu recorde? 🎯",
        "A Leaderboard não se conquista a descansar! Volta ao jogo! 💪",
        "Sentes falta da adrenalina do Higher or Lower? Nós também sentimos a tua falta! 🎬",
        "Ainda tens o que é preciso? Prova isso no Higher or Lower! 😏",
        "O jogo está à tua espera. Quanto tempo consegues aguentar? ⏱️",
        "Hoje pode ser o dia em que bates o teu recorde! Vai lá tentar! 🚀",
        "A tua posição na Leaderboard depende de ti. Não a deixes escapar! 🔥"
    ];
}
