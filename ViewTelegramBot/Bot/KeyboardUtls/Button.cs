namespace ViewTelegramBot.Bot.KeyboardUtls;

/// <summary>
/// Кнопка для клавиатуры Telegram-бота.
/// </summary>
public class Button
{
    /// <summary>
    /// Текст кнопки.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Дополнительные данные (payload) кнопки.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Создаёт кнопку с текстом и payload.
    /// </summary>
    /// <param name="text">Текст кнопки</param>
    /// <param name="payload">Payload</param>
    public Button(string text, string payload) => (Text, Payload) = (text, payload);

    /// <summary>
    /// Создаёт кнопку только с текстом.
    /// </summary>
    /// <param name="text">Текст кнопки</param>
    public Button(string text) => Text = text;
}
