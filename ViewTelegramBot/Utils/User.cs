using System.Text.Json.Serialization;

namespace ViewTelegramBot.Utils;

/// <summary>
/// Модель пользователя для Telegram-бота.
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Telegram ID пользователя.
    /// </summary>
    [JsonPropertyName("telegram_id")]
    public long TelegramId { get; set; }

    /// <summary>
    /// ID пользователя в системе.
    /// </summary>
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// Токен пользователя для авторизации.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Язык пользователя (по умолчанию "ru").
    /// </summary>
    public string Lang { get; set; } = "ru";
}
