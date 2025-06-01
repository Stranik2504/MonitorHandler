using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;
using ViewTelegramBot.Utils;
using ViewTelegramBot.Utils.Models;

namespace ViewTelegramBot.Commands;

/// <summary>
/// Команда для управления серверами пользователя через Telegram-бота.
/// </summary>
[Names("servers")]
[Visibility(Visibility.Visible)]
[TypeEvent(TypeEvents.Text, TypeEvents.Callback)]
public class ServersCommand : Command
{
    /// <summary>
    /// Отображает главное меню управления серверами.
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

    /// <summary>
    /// Очищает состояние и параметры пользователя и возвращает в главное меню серверов.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("clear")]
    public async Task ClearAndDefaultMenu(Context ctx)
    {
        await ctx.ClearState();
        await ctx.ClearParams();

        await DefaultMenu(ctx);
    }

    /// <summary>
    /// Отображает список серверов пользователя для выбора.
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

    /// <summary>
    /// Отображает информацию о выбранном сервере.
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

        var ramText = ctx.PhrasesManager.Insert(
            "text_ram",
            metric.UseRam.ConvertToNormalViewBytes(ctx.PhrasesManager),
            metric.TotalRam.ConvertToNormalViewBytes(ctx.PhrasesManager)
        );

        var cpusUsage = string.Join(
            "\n\n",
            metric.Cpus.Select(
                (cpu, index) => MakeProgressView(
                    ctx.PhrasesManager,
                    cpu,
                    ctx.PhrasesManager["text_core"] + " " + (index + 1) + ":"
                )
            )
        );

        var ramUse = MakeProgressView(
            ctx.PhrasesManager,
            metric.UseRam,
            metric.TotalRam
        );

        var disksUse = string.Join(
            "\n\n",
            metric.UseDisks.Select(
                (disk, index) => MakeProgressView(
                    ctx.PhrasesManager,
                    disk,
                    metric.TotalDisks[index],
                    ctx.PhrasesManager.Insert(
                        "text_disk",
                        " " + (index + 1),
                        disk.ConvertToNormalViewBytes(ctx.PhrasesManager),
                        metric.TotalDisks[index].ConvertToNormalViewBytes(ctx.PhrasesManager)
                    ) + ":"
                )
            )
        );

        var text = ctx.PhrasesManager.Insert(
            "text_server_info",
            server?.Name.ConvertToMark2() ?? "",
            server?.Ip.ConvertToMark2() ?? "",
            cpusUsage.ConvertToMark2(),
            ramText.ConvertToMark2(),
            ramUse.ConvertToMark2(),
            disksUse.ConvertToMark2(),
            metric.NetworkSend.ConvertToNormalViewBytes(ctx.PhrasesManager).ConvertToMark2(),
            metric.NetworkReceive.ConvertToNormalViewBytes(ctx.PhrasesManager).ConvertToMark2()
        );

        await ctx.Edit(
            text,
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_docker"], $"docker:default:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_scripts"], $"servers:scripts:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_info"], $"servers:info:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_refresh"], $"servers:server:{serverId}")),
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:choose"))
            ),
            ParseMode.MarkdownV2
        );
    }

    /// <summary>
    /// Отображает скрипты сервера (заглушка).
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("scripts")]
    public async Task ViewServerScripts(Context ctx)
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

        await ctx.Edit(
            ctx.PhrasesManager["text_scripts_not_implemented"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:server:" + serverId))
            ),
            ParseMode.MarkdownV2
        );
    }

    /// <summary>
    /// Отображает информацию о сервере (токен).
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("info")]
    public async Task ViewServerInfo(Context ctx)
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
        httpClient.DefaultRequestHeaders.Add("userToken", user.Token);

        var response = await httpClient.GetAsync($"{Program.Config?.ServerHost}/api/v1/servers/token?id={serverId}");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        var token = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_get_server"]);
            await ctx.Answer();
            return;
        }

        var text = ctx.PhrasesManager.Insert(
            "text_server_added",
            await response.Content.ReadAsStringAsync()
        );

        await ctx.Edit(
            text,
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:server:" + serverId))
            ),
            ParseMode.MarkdownV2
        );
    }

    /// <summary>
    /// Запускает процесс добавления нового сервера (запрос имени).
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

    /// <summary>
    /// Устанавливает имя нового сервера и запрашивает IP.
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

    /// <summary>
    /// Устанавливает IP нового сервера и завершает добавление.
    /// </summary>
    /// <param name="ctx">Контекст</param>
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

    /// <summary>
    /// Запускает процесс добавления существующего сервера (запрос токена).
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("add_exists")]
    public async Task AddExistsServer(Context ctx)
    {
        await ctx.Edit(
            ctx.PhrasesManager["text_enter_server_token"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:clear"))
            ),
            saveContent: false
        );
        await ctx.Answer();

        await ctx.SetState("servers:add_exists_token");
    }

    /// <summary>
    /// Устанавливает токен существующего сервера и запрашивает IP.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [State("add_exists_token")]
    public async Task SetExistsServerToken(Context ctx)
    {
        if (string.IsNullOrEmpty(ctx.Text.Trim()))
        {
            await ctx.Send(ctx.PhrasesManager["text_error_token"]);
            return;
        }

        await ctx.AddParam("token", ctx.Text.Trim());

        await ctx.Send(
            ctx.PhrasesManager["text_enter_exists_server_ip"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:clear"))
            ),
            saveContent: false
        );

        await ctx.SetState("servers:add_exists_ip");
    }

    /// <summary>
    /// Устанавливает IP существующего сервера и завершает добавление.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [State("add_exists_ip")]
    public async Task SetExistsServerIp(Context ctx)
    {
        var ip = ctx.Text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            await ctx.Send(ctx.PhrasesManager["text_error_ip"]);
            return;
        }

        var userParams = await ctx.GetParams();

        if (!userParams.Success || !userParams.Params.TryGetValue("token", out var token))
        {
            await ctx.ClearParams();
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        var user = await ctx.GetUser();

        if (user == null)
        {
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["user_not_found"], saveContent: false);
            return;
        }

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("userId", user.Id.ToString());
        httpClient.DefaultRequestHeaders.Add("userToken", user.Token);

        var response = await httpClient.PostAsJsonAsync(
            $"{Program.Config?.ServerHost}/api/v1/servers/add",
            new { Token = token, Ip = ip }
        );

        if (!response.IsSuccessStatusCode)
        {
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        var server = await response.Content.ReadFromJsonAsync<Server>();

        if (server == null)
        {
            await ctx.ClearState();
            await ctx.Send(ctx.PhrasesManager["text_error_to_add_server"], saveContent: false);
            return;
        }

        await ctx.ClearState();
        await ctx.Send(
            ctx.PhrasesManager.Insert("text_server_added", server.Name.ConvertToMark2()),
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:default"))
            ),
            saveContent: false
        );
    }

    /// <summary>
    /// Запускает процесс удаления сервера (выбор сервера).
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("delete")]
    public async Task DeleteServer(Context ctx)
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
                    $"servers:delete_confirm:{server.Id}"
                )
            );
        }

        keyboard.AddLine(
            new Button(ctx.PhrasesManager["button_back"], "servers:default")
        );

        await ctx.Edit(
            ctx.PhrasesManager["text_choose_server_to_delete"],
            keyboard,
            ParseMode.MarkdownV2
        );
    }

    /// <summary>
    /// Подтверждает удаление сервера.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    [CallbackState("delete_confirm")]
    public async Task ConfirmDeleteServer(Context ctx)
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

        var response = await httpClient.DeleteAsync($"{Program.Config?.ServerHost}/api/v1/servers/{serverId}");

        if (!response.IsSuccessStatusCode)
        {
            await ctx.Edit(ctx.PhrasesManager["text_error_to_delete_server"]);
            await ctx.Answer();
            return;
        }

        await ctx.Edit(
            ctx.PhrasesManager["text_server_deleted"],
            new Keyboard(
                new Line(new Button(ctx.PhrasesManager["button_back"], "servers:default"))
            ),
            ParseMode.MarkdownV2
        );
    }

    /// <summary>
    /// Проверяет корректность IP-адреса.
    /// </summary>
    /// <param name="ip">IP-адрес</param>
    /// <returns>True, если IP корректен</returns>
    private static bool IsValidIp(string ip)
    {
        var regex = new Regex(@"^((25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])\.){3}(25[0-5]|2[0-4][0-9]|1[0-9]{2}|[1-9]?[0-9])$");
        return regex.IsMatch(ip);
    }

    private static string MakeProgressView(PhrasesManager phrasesManager, double value, double total, string name = "")
    {
        if (total <= 0) return phrasesManager["text_no_data"];

        var percent = (value / total) * 100;
        return MakeProgressView(phrasesManager, percent, name);
    }

    private static string MakeProgressView(PhrasesManager phrasesManager, double percent, string name = "")
    {
        if (percent <= 0) return phrasesManager["text_no_data"];

        var progressBar = new string('█', (int)(percent / 5)) + new string('░', 20 - (int)(percent / 5));
        return phrasesManager.Insert(string.IsNullOrWhiteSpace(name) ? "text_progress_bar_min" : "text_progress_bar", name, progressBar, percent.ToString("F2"));
    }
}
