namespace ViewTelegramBot.Utils;

/// <summary>
/// Класс конфигурации для Telegram-бота и подключения к серверу/БД.
/// </summary>
public class Config
{
    /// <summary>
    /// Имя пользователя по умолчанию, если не задано.
    /// </summary>
    public string UserNoHaveUsername { get; set; } = "noname";

    /// <summary>
    /// Признак необходимости отправки запроса администратору.
    /// </summary>
    public bool SendRequestToAdmin { get; set; } = false;

    /// <summary>
    /// Токен Telegram-бота.
    /// </summary>
    public string TelegramToken { get; set; } = string.Empty;

    /// <summary>
    /// Хост основной базы данных.
    /// </summary>
    public string MainDbHost { get; set; } = string.Empty;

    /// <summary>
    /// Порт основной базы данных.
    /// </summary>
    public int MainDbPort { get; set; } = 0;

    /// <summary>
    /// Имя основной базы данных.
    /// </summary>
    public string MainDbName { get; set; } = string.Empty;

    /// <summary>
    /// Имя пользователя для подключения к основной базе данных.
    /// </summary>
    public string MainDbUser { get; set; } = string.Empty;

    /// <summary>
    /// Пароль пользователя для подключения к основной базе данных.
    /// </summary>
    public string MainDbPassword { get; set; } = string.Empty;

    /// <summary>
    /// Хост сервера.
    /// </summary>
    public string ServerHost { get; set; } = string.Empty;
}
