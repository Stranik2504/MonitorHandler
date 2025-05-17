namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст состояния пользователя (например, для FSM).
/// </summary>
public class State
{
    /// <summary>
    /// Имя команды.
    /// </summary>
    public string NameCommand { get; set; } = "";

    /// <summary>
    /// Имя метода.
    /// </summary>
    public string? NameMethod { get; set; }

    /// <summary>
    /// Параметры состояния.
    /// </summary>
    public string? Params { get; set; }
}
