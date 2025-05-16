using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Commands;

namespace ViewTelegramBot.Attributes;

public abstract class PreconditionAttribute : Attribute
{
    public abstract Task<PreconditionResult> CheckPermissionsAsync(Command command, Context? context);
}