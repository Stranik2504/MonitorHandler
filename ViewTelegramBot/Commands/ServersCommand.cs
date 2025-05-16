using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Utils.Models;

namespace ViewTelegramBot.Commands;

[Names("servers")]
[Visibility(Visibility.Visible)]
[TypeEvent(TypeEvents.Text, TypeEvents.Callback)]
public class ServersCommand : Command
{
    [CallbackState("default")]
    public async Task DefaultMenu(Context ctx)
    {
        await ctx.Edit(
            ctx.PhrasesManager["text_servers"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_choose_server"], "servers:choose")),
                new Line(new Button(ctx.PhrasesManager["button_add_server"], "servers:add")),
                new Line(new Button(ctx.PhrasesManager["button_add_exists_server"], "servers:add_exists")),
                new Line(new Button(ctx.PhrasesManager["button_delete_server"], "servers:delete")),
                new Line(new Button(ctx.PhrasesManager["button_back"], "start:default"))
            )
        );
        await ctx.Answer();
    }

    [CallbackState("clear")]
    public async Task ClearAndDefaultMenu(Context ctx)
    {
        await ctx.ClearState();
        await ctx.ClearParams();

        await DefaultMenu(ctx);
    }

    [CallbackState("choose")]
    public async Task ChooseServer(Context ctx)
    {
        var user = await ctx.GetUser();

        if (user == null)
        {
            await ctx.Edit(ctx.PhrasesManager["user_not_found"]);
            await ctx.Answer();
            return;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("userId", user.Id.ToString());
        httpClient.DefaultRequestHeaders.Add("token", user.Token);

        var response = await httpClient.GetAsync($"{Program.Config?.ServerHost}/api/v1/servers");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_servers"]);
            await ctx.Answer();
            return;
        }

        var servers = response.Content.ReadFromJsonAsAsyncEnumerable<Server>();
        var keyboard = new Keyboard();

        await foreach (var server in servers)
        {
            if (server == null) continue;

            keyboard.AddLine(
                new Button(
                    server.Name,
                    $"servers:server:{server.Id}"
                )
            );
        }

        keyboard.AddLine(
            new Button(ctx.PhrasesManager["button_back"], "servers:default")
        );

        await ctx.Edit(
            ctx.PhrasesManager["text_choose_server"],
            keyboard,
            ParseMode.MarkdownV2
        );
        await ctx.Answer();
    }

    [CallbackState("server")]
    public async Task ViewServer(Context ctx)
    {
        var user = await ctx.GetUser();

        if (user == null)
        {
            await ctx.Edit(ctx.PhrasesManager["user_not_found"]);
            await ctx.Answer();
            return;
        }

        var serverId = ctx.CallbackState?.Params;

        if (string.IsNullOrEmpty(serverId))
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("userId", user.Id.ToString());
        httpClient.DefaultRequestHeaders.Add("token", user.Token);

        var response = await httpClient.GetAsync($"{Program.Config?.ServerHost}/api/v1/server/metrics?serverId={serverId}");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        var metric = await response.Content.ReadFromJsonAsync<Metric>();

        if (metric == null)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        response = await httpClient.GetAsync($"{Program.Config?.ServerHost}/api/v1/servers");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        var servers = response.Content.ReadFromJsonAsAsyncEnumerable<Server>();

        var server = await servers.First(x => x?.Id == serverId.ToInt());

        var text = ctx.PhrasesManager.Insert(
            "text_server_info",
            server?.Name ?? "",
            server?.Ip ?? "",
            metric.Cpus[0].ToString("F2"),
            metric.UseRam.ToString("F2"),
            metric.TotalRam.ToString("F2"),
            metric.UseDisks[0].ToString(),
            metric.TotalDisks[0].ToString(),
            metric.NetworkSend.ToString(),
            metric.NetworkReceive.ToString()
        );

        await ctx.Edit(
            text.ConvertToMark2(),
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_docker"], $"servers:docker:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_scripts"], $"servers:scripts:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:choose"))
            ),
            ParseMode.MarkdownV2
        );
    }

    [CallbackState("add")]
    public async Task AddServer(Context ctx)
    {
        await ctx.Edit(
            ctx.PhrasesManager["text_enter_server_name"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:clear"))
            ),
            saveContent: false
        );
        await ctx.Answer();

        await ctx.SetState("servers:add_name");
    }

    [State("add_name")]
    public async Task SetNameServer(Context ctx)
    {
        await ctx.AddParam("name", ctx.Text);

        await ctx.Send(
            ctx.PhrasesManager["text_enter_server_ip"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:clear"))
            ),
            saveContent: false
        );

        await ctx.SetState("servers:add_ip");
    }

    [State("add_ip")]
    public async Task SetIpServer(Context ctx)
    {
        if (!IsValidIp(ctx.Text))
        {
            await ctx.Send(ctx.PhrasesManager["text_error_ip"]);
            return;
        }

        var userParams = await ctx.GetParams();

        if (!userParams.Success || !userParams.Params.TryGetValue("name", out var name))
        {
            await ctx.ClearParams();
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        var user = await ctx.GetUser();

        if (user == null)
        {
            await ctx.ClearParams();
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["user_not_found"], saveContent: false);
            return;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("userId", user.Id.ToString());
        httpClient.DefaultRequestHeaders.Add("userToken", user.Token);

        var response = await httpClient.PostAsJsonAsync(
            $"{Program.Config?.ServerHost}/api/v1/servers",
            new ServerInfo() { Name = name, Ip = ctx.Text }
        );

        if (!response.IsSuccessStatusCode)
        {
            await ctx.ClearParams();
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        var token = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrEmpty(token))
        {
            await ctx.ClearParams();
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        await ctx.ClearParams();
        await ctx.ClearState();
        await ctx.Send(
            ctx.PhrasesManager.Insert("text_server_added", token),
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:default"))
            ),
            saveContent: false
        );

    }

    private static bool IsValidIp(string ip)
    {
        var regex = new Regex(@"^((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])$");
        return regex.IsMatch(ip);
    }
}