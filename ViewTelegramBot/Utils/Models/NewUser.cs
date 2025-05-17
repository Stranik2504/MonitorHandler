namespace ViewTelegramBot.Utils.Models;

/// <summary>
/// Модель нового пользователя для Telegram-бота.
/// </summary>
public class NewUser
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя пользователя.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    /// Токен пользователя для авторизации.
    /// </summary>
    public string Token { get; set; }
}
