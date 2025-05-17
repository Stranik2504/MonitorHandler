namespace ViewTelegramBot.Bot.Contexts;

/// <summary>
/// Контекст callback-запроса пользователя.
/// </summary>
public class Callback
{
    /// <summary>
    /// Идентификатор callback-запроса.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Текст callback-запроса.
    /// </summary>
    public string? Text { get; private set; }

    /// <summary>
    /// Пользователь, отправивший callback.
    /// </summary>
    public User User { get; set; }

    /// <summary>
    /// Создаёт новый callback-контекст.
    /// </summary>
    /// <param name="id">Идентификатор callback</param>
    /// <param name="text">Текст callback</param>
    /// <param name="user">Пользователь</param>
    public Callback(string id, string? text, User user) => (Id, Text, User) = (id, text ?? "", user);

    /// <summary>
    /// Устанавливает текст callback.
    /// </summary>
    /// <param name="text">Текст</param>
    public void SetText(string text) => Text = text;
}
