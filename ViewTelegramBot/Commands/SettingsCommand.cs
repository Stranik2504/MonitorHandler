using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Commands;

[Names("settings")]
[Visibility(Visibility.Visible)]
[TypeEvent(TypeEvents.Text, TypeEvents.Callback)]
public class SettingsCommand : Command
{
    [CallbackState("default")]
    public async Task DefaultState(Context ctx)
    {
        await ctx.Edit(
            ctx.PhrasesManager["text_settings"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_change_lang"], "settings:change_lang")),
                new Line(new Button(ctx.PhrasesManager["button_token"], "settings:get_token")),
                new Line(new Button(ctx.PhrasesManager["button_back"], "start:default"))
            ),
            parseMode: ParseMode.MarkdownV2
        );
        await ctx.Answer();
    }

    [CallbackState("get_token")]
    public async Task GetToken(Context ctx)
    {
        var user = await ctx.Local.GetUser(ctx.UserId);

        if (user == null)
        {
            await ctx.Edit(ctx.PhrasesManager["user_not_found"]);
            await ctx.Answer();
            return;
        }

        await ctx.Edit(
            ctx.PhrasesManager.Insert("text_token", user.Token.ConvertToMark2()),
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "settings:default"))
            ),
            parseMode: ParseMode.MarkdownV2
        );
        await ctx.Answer();
    }

    [CallbackState("change_lang")]
    public async Task ChangeLang(Context ctx)
    {
        var user = await ctx.Local.GetUser(ctx.UserId);

        if (user == null)
        {
            await ctx.Edit(ctx.PhrasesManager["user_not_found"]);
            await ctx.Answer();
            return;
        }

        var buttons = PhrasesLoader.GetLangs().Select(x =>
            new Line(new Button(PhrasesLoader.GetLangName(x), "settings:set_lang:" + x))
        ).ToList();

        buttons.Add(
            new Line(new Button(ctx.PhrasesManager["button_back"], "settings:default"))
        );

        await ctx.Edit(
            ctx.PhrasesManager["text_change_lang"],
            new Keyboard(buttons),
            parseMode: ParseMode.MarkdownV2
        );
        await ctx.Answer();
    }

    [CallbackState("set_lang")]
    public async Task SetNewLang(Context ctx)
    {
        var param = ctx.CallbackState?.Params ?? "default";

        await ctx.Local.SetLangUser(ctx.UserId, param);

        await ctx.Edit(
            ctx.PhrasesManager.Insert("text_lang_changed", PhrasesLoader.GetLangName(param)),
            parseMode: ParseMode.MarkdownV2
        );
        await ctx.Answer();

        await ctx.ReloadPhraseManager();

        await StartCommand.StartMenu(ctx, TypeEvents.Text);
    }
}