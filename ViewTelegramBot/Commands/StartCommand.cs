using System.Net.Http.Json;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Utils.Models;

namespace ViewTelegramBot.Commands;

/// <summary>
/// Команда запуска и регистрации пользователя в Telegram-боте.
/// </summary>
[Names("start")]
[Visibility(Visibility.Hidden)]
[TypeEvent(TypeEvents.Text, TypeEvents.Callback)]
public class StartCommand : Command
{
    /// <summary>
    /// Обрабатывает стартовое состояние пользователя.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [DefaultState]
    public async Task Start(Context ctx)
    {
        var user = await ctx.Local.GetUser(ctx.UserId);

        if (user != null)
        {
            await StartMenu(ctx, TypeEvents.Text);
            return;
        }

        var userName = ctx.Content?.User.Username ?? Program.Config?.UserNoHaveUsername ?? ctx.UserId.ToString();

        var client = new HttpClient();
        var response = await client.PostAsJsonAsync($"{Program.Config?.ServerHost}/api/v1/register", userName);

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Send("Error: " + response.StatusCode);
            return;
        }

        var newUser = await response.Content.ReadFromJsonAsync<NewUser>();

        if (newUser == null)
        {
            await ctx.Send("Error: User is null");
            return;
        }

        await ctx.Local.AddUser(
            ctx.UserId,
            newUser.Id,
            newUser.Token,
            "ru"
        );

        await StartMenu(ctx, TypeEvents.Text);
    }

    /// <summary>
    /// Обрабатывает стартовый callback.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("default")]
    public async Task StartCallback(Context ctx)
    {
        await StartMenu(ctx, TypeEvents.Callback);
    }

    /// <summary>
    /// Отображает главное меню Telegram-бота.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    /// <param name="typeEvent">Тип события</param>
    public static async Task StartMenu(Context ctx, TypeEvents typeEvent)
    {
        var text = ctx.PhrasesManager["text_menu"];
        var keyboard = new Keyboard
        (
            new Line(new Button(ctx.PhrasesManager["button_servers"], "servers:default")),
            new Line(new Button(ctx.PhrasesManager["button_settings"], "settings:default"))
        );

        if (typeEvent == TypeEvents.Callback)
        {
            await ctx.Edit(
                text,
                keyboard,
                ParseMode.MarkdownV2
            );

            return;
        }

        await ctx.Send(
            text,
            keyboard,
            ParseMode.MarkdownV2
        );
    }
}
