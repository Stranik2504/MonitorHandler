namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст чата Telegram.
/// </summary>
public class Chat
{
    /// <summary>
    /// Идентификатор чата.
    /// </summary>
    public long Id { get; }

    /// <summary>
    /// Создаёт новый контекст чата.
    /// </summary>
    /// <param name="id">Идентификатор чата</param>
    public Chat(long id) => Id = id;
}
