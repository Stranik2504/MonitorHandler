using ViewTelegramBot.Bot.Phrase;

namespace ViewTelegramBot.Attributes;

public class PreconditionResult(bool success, string reason)
{
    public bool Success { get; } = success;
    public string Reason { get; } = reason;

    public static Task<PreconditionResult> FromSuccess() => new(() => new PreconditionResult(true, string.Empty));
    public static Task<PreconditionResult> FromError(string reason) => new(() => new PreconditionResult(false, reason));
    public static Task<PreconditionResult> FromError() => FromError(string.Empty);
}