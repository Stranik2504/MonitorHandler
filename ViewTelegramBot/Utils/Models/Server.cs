namespace ViewTelegramBot.Utils.Models;

/// <summary>
/// Модель сервера для Telegram-бота.
/// </summary>
public class Server
{
    /// <summary>
    /// Уникальный идентификатор сервера.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя сервера.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// IP-адрес сервера.
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// Текущий статус сервера.
    /// </summary>
    public string Status { get; set; }
}
