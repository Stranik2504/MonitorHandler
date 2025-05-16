using System.Text.Json.Serialization;

namespace ViewTelegramBot.Utils;

public class User
{
    public int Id { get; set; }

    [JsonPropertyName("telegram_id")]
    public long TelegramId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    public string Token { get; set; }

    public string Lang { get; set; } = "ru";
}