using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Commands;

/// <summary>
/// Команда по умолчанию для обработки неизвестных или некорректных команд.
/// </summary>
[DefaultClass]
[Visibility(Visibility.Visible)]
[TypeEvent(TypeEvents.Text)]
public class DefaultCommand : Command
{
    /// <summary>
    /// Состояние по умолчанию, отправляет сообщение об ошибке и возвращает в меню серверов.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [DefaultState]
    public async Task DefaultState(Context ctx)
    {
        await ctx.Send(ctx.PhrasesManager["error_try_again"]);
        await new ServersCommand().DefaultMenu(ctx);
    }
}
