using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Commands;

namespace ViewTelegramBot.Attributes;

/// <summary>
/// Абстрактный атрибут-предусловие для проверки прав доступа к команде.
/// </summary>
public abstract class PreconditionAttribute : Attribute
{
    /// <summary>
    /// Проверяет права доступа к команде.
    /// </summary>
    /// <param name="command">Команда</param>
    /// <param name="context">Контекст</param>
    /// <returns>Результат проверки предусловия</returns>
    public abstract Task<PreconditionResult> CheckPermissionsAsync(Command command, Context? context);
}
