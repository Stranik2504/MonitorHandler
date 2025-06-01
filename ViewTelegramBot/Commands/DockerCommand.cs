using System.Net.Http.Json;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Commands;

/// <summary>
/// Команда для управления Docker-контейнерами и образами.
/// </summary>
[Names("docker")]
[Visibility(Visibility.Visible)]
[TypeEvent(TypeEvents.Text, TypeEvents.Callback)]
public class DockerCommand : Command
{
    /// <summary>
    /// Отображает список контейнеров и образов Docker для выбранного сервера.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("default")]
    public async Task DefaultState(Context ctx)
    {
        // Получение списка контейнеров Docker api/v1/server/docker/{serverId}/containers и образов api/v1/server/docker/{serverId}/images
        var serverId = ctx.CallbackState?.Params;

        if (string.IsNullOrEmpty(serverId))
        {
            await ctx.Edit(ctx.PhrasesManager["error_try_again"]);
            await ctx.Answer();
            return;
        }

        var user = await ctx.Local.GetUser(ctx.UserId);

        if (user == null)
        {
            await ctx.Edit(ctx.PhrasesManager["user_not_found"]);
            await ctx.Answer();
            return;
        }

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Token", user.Token);
        client.DefaultRequestHeaders.Add("UserId", user.UserId.ToString());

        var containersResponse = await client.GetAsync($"{Program.Config?.ServerHost}/api/v1/server/docker/{serverId}/containers");

        if (!containersResponse.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["error_try_again"]);
            await ctx.Answer();
            return;
        }

        var containers = await containersResponse.Content.ReadFromJsonAsync<List<DockerContainer>>();

        if (containers == null)
        {
            await ctx.Edit(ctx.PhrasesManager["docker_no_containers"]);
            await ctx.Answer();
            return;
        }

        var imagesResponse = await client.GetAsync($"{Program.Config?.ServerHost}/api/v1/server/docker/{serverId}/images");

        if (!imagesResponse.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["error_try_again"]);
            await ctx.Answer();
            return;
        }

        var images = await imagesResponse.Content.ReadFromJsonAsync<List<DockerImage>>();

        if (images == null)
        {
            await ctx.Edit(ctx.PhrasesManager["docker_no_images"]);
            await ctx.Answer();
            return;
        }

        var text = ctx.PhrasesManager["docker_containers"];

        foreach (var container in containers)
        {
            text += $"{container.Id} \\- {container.Name.ConvertToMark2().Replace(@"\\", "\\")} \\({container.Status}\\)\n";
        }

        text += "\n" + ctx.PhrasesManager["docker_images"];

        foreach (var image in images)
        {
            text += $"{image.Id} \\- {image.Name.ConvertToMark2().Replace(@"\\", "\\")} \\({image.Hash}\\)\n";
        }

        await ctx.Edit(
            text,
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:default"))
            ),
            parseMode: ParseMode.MarkdownV2
        );
    }
}
