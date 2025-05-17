using ViewTelegramBot.Bot.Phrase;

namespace ViewTelegramBot.Attributes;

/// <summary>
/// Результат проверки предусловия для команды.
/// </summary>
public class PreconditionResult(bool success, string reason)
{
    /// <summary>
    /// Признак успешности проверки.
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// Причина ошибки (если есть).
    /// </summary>
    public string Reason { get; } = reason;

    /// <summary>
    /// Возвращает успешный результат.
    /// </summary>
    public static Task<PreconditionResult> FromSuccess() => new(() => new PreconditionResult(true, string.Empty));

    /// <summary>
    /// Возвращает результат с ошибкой и причиной.
    /// </summary>
    public static Task<PreconditionResult> FromError(string reason) => new(() => new PreconditionResult(false, reason));

    /// <summary>
    /// Возвращает результат с ошибкой без причины.
    /// </summary>
    public static Task<PreconditionResult> FromError() => FromError(string.Empty);
}
