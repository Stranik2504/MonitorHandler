namespace ViewTelegramBot.Utils;

/// <summary>
/// Места хранения пользовательских данных.
/// </summary>
public enum Place
{
    /// <summary>
    /// Состояния.
    /// </summary>
    State,
    /// <summary>
    /// Параметры.
    /// </summary>
    Params,
    /// <summary>
    /// Пользователь.
    /// </summary>
    User
}

/// <summary>
/// Типы событий Telegram-бота.
/// </summary>
public enum TypeEvents
{
    /// <summary>
    /// Текстовое сообщение.
    /// </summary>
    Text,
    /// <summary>
    /// Callback-запрос.
    /// </summary>
    Callback
}

/// <summary>
/// Уровни видимости.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Видимый.
    /// </summary>
    Visible,
    /// <summary>
    /// Скрытый.
    /// </summary>
    Hidden,
    /// <summary>
    /// Свёрнутый.
    /// </summary>
    Collapsed
}

/// <summary>
/// Режимы парсинга сообщений Telegram.
/// </summary>
public enum ParseMode
{
    /// <summary>
    /// HTML.
    /// </summary>
    Html = 0,
    /// <summary>
    /// Markdown.
    /// </summary>
    Markdown = 1,
    /// <summary>
    /// MarkdownV2.
    /// </summary>
    MarkdownV2 = 2
}
