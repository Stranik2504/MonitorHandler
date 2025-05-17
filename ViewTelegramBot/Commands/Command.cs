using System.Reflection;
using System.Runtime.CompilerServices;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Commands;

/// <summary>
/// Абстрактный базовый класс для всех команд Telegram-бота.
/// </summary>
public abstract class Command
{
    /// <summary>
    /// Префикс команды (например, "/").
    /// </summary>
    private const string CommandPrefix = "/";

    /// <summary>
    /// Массив поддерживаемых типов событий для команды.
    /// </summary>
    private TypeEvents[]? _typeEvents;

    /// <summary>
    /// Уровень видимости команды.
    /// </summary>
    private Visibility _visibility;

    /// <summary>
    /// Описание команды.
    /// </summary>
    private string? _description;

    /// <summary>
    /// Список имён команды.
    /// </summary>
    private List<string> _names = [];

    /// <summary>
    /// Список разрешённых уровней доступа.
    /// </summary>
    private List<long> _accesses = [];

    /// <summary>
    /// Основное имя команды.
    /// </summary>
    protected string Name => string.IsNullOrWhiteSpace(_names[0]) ? "default" : _names[0];

    /// <summary>
    /// Коллекция всех команд.
    /// </summary>
    private static IReadOnlyCollection<Command?>? Commands { get; }

    /// <summary>
    /// Статический конструктор для инициализации списка команд.
    /// </summary>
    static Command()
    {
        Commands = Assembly.GetAssembly(typeof(Command))?.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Command))).ToList()
            .Select(x => Activator.CreateInstance(x) as Command).ToList().AsReadOnly();
    }

    /// <summary>
    /// Конструктор команды, инициализирует атрибуты.
    /// </summary>
    protected Command()
    {
        var attributes = GetType().GetCustomAttributes(false);

        attributes.ForEach(att =>
        {
            switch (att)
            {
                case VisibilityAttribute vis:
                    _visibility = vis.Visibility;
                    break;
                case TypeEventAttribute typ:
                    _typeEvents = typ.TypeEvents;
                    break;
                case DescriptionAttribute dis:
                    _description = dis.Description;
                    break;
                case NamesAttribute nm:
                    _names = nm.Names.ToList();
                    break;
                case AccessesAttribute acc:
                    _accesses = acc.Accesses.ToList();
                    break;
            }
        });
    }

    /// <summary>
    /// Выполняет команду по контексту.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    public static async Task ExecuteAsync(Context ctx)
    {
        string nameCommand;
        string nameMethod;

        await ctx.LoadParams();

        if (ctx is { Type: TypeEvents.Callback, CallbackState: not null })
        {
            nameCommand = ctx.CallbackState.NameCommand;
            nameMethod = ctx.CallbackState?.NameMethod ?? string.Empty;
        }
        else if (ctx.State == null)
        {
            nameCommand =  "default";
            nameMethod = "default";

            if (ctx.Content != null && ctx.Content.Text.Contains(CommandPrefix))
            {
                nameCommand = ctx.Content.Text.Split(" ")[0].Replace(CommandPrefix, "");
                ctx.Content.SetText(ctx.Content.Text.Replace(CommandPrefix + nameCommand + " ", ""));
            }
        }
        else
        {
            nameCommand = ctx.State.NameCommand;
            nameMethod = ctx.State?.NameMethod ?? string.Empty;
        }

        var command = GetCommandByName(nameCommand, ctx.Type, ctx.Access);

        if (command == null)
        {
            await NotFound(ctx);
            return;
        }

        var method = command.GetType().GetMethods().GetByState(nameMethod, ctx.Type);

        if (method == null)
        {
            await NotFound(ctx);
            return;
        }

        if (command._accesses.Count > 0 && !command._accesses.Contains(ctx.Access))
        {
            await NotAccess(ctx, command._accesses.Min());
            return;
        }

        var precondition = await CheckPreconditions(command, ctx);

        if (!precondition.Success)
        {
            var text = string.IsNullOrWhiteSpace(precondition.Reason) ?
                ctx.PhrasesManager["error_to_get_access"] :
                precondition.Reason;

            await NotPredict(ctx, text);
            return;
        }

        await ExecuteMethod(command, method, ctx);
    }

    /// <summary>
    /// Выполняет команду по умолчанию.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    protected static async Task DefaultCommand(Context? ctx)
    {
        var command = Commands?.FirstOrDefault(x => x?.GetType().GetCustomAttributes().HaveAttribute<DefaultClassAttribute>() ?? false);
        var method = command?.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes().HaveAttribute<DefaultStateAttribute>());
        await ExecuteMethod(command, method, ctx);
    }

    /// <summary>
    /// Выполняет команду по умолчанию с заданным состоянием и типом события.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    /// <param name="state">Состояние</param>
    /// <param name="type">Тип события</param>
    protected static async Task DefaultCommand(Context? ctx, string state, TypeEvents type)
    {
        var command = Commands?.FirstOrDefault(x => x?.GetType().GetCustomAttributes().HaveAttribute<DefaultClassAttribute>() ?? false);
        var method = command?.GetType().GetMethods().GetByState(state, type);
        await ExecuteMethod(command, method, ctx);
    }

    /// <summary>
    /// Выполняет указанный метод команды.
    /// </summary>
    /// <param name="command">Команда</param>
    /// <param name="method">Метод</param>
    /// <param name="ctx">Контекст</param>
    private static async Task ExecuteMethod(Command? command, MethodBase? method, Context? ctx)
    {
        try
        {
            if (command == null || method == null)
                return;

            object? param = null;

            if (method.GetParameters().HaveParam<Context>())
                param = ctx;

            dynamic result = method.Invoke(command, param != null ? [param] : null)!;

            if (result == null)
                return;

            if (method.GetCustomAttributes().HaveAttribute<AsyncStateMachineAttribute>())
                await result;
        }
        catch (Exception ex)
        {
            ex.Log();
        }
    }

    /// <summary>
    /// Отправляет сообщение об ошибке.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    private static async Task Error(Context? ctx)
    {
        var text = ctx?.PhrasesManager["error"];

        if (ctx == null || string.IsNullOrWhiteSpace(text))
            return;

        if (ctx is { Type: TypeEvents.Callback, Callback: not null })
            await ctx.Edit(text);
        else
            await ctx.Send(text);
    }

    /// <summary>
    /// Отправляет сообщение о недостаточном уровне доступа.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    /// <param name="access">Требуемый уровень доступа</param>
    private static async Task NotAccess(Context? ctx, long access)
    {
        var text = ctx?.PhrasesManager["error_to_get_access"] ?? string.Empty;
        Keyboard? keyboard = null;

        if (Program.Config?.SendRequestToAdmin ?? false)
            keyboard = new Keyboard(
                true,
                new Button(
                    ctx?.PhrasesManager["get_access_button"] ?? string.Empty,
                    $"request_access:request:{access}"
                )
            );

        if (ctx == null)
            return;

        if (ctx is { Type: TypeEvents.Callback, Callback: not null })
            await ctx.Edit(text, keyboard);
        else
            await ctx.Send(text, keyboard);
    }

    /// <summary>
    /// Отправляет сообщение о невозможности выполнить предусловие.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    /// <param name="message">Сообщение</param>
    private static async Task NotPredict(Context? ctx, string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || ctx == null)
            return;

        if (ctx is { Type: TypeEvents.Callback, Callback: not null })
            await ctx.Edit(message);
        else
            await ctx.Send(message);
    }

    /// <summary>
    /// Отправляет сообщение о том, что команда не найдена.
    /// </summary>
    /// <param name="ctx">Контекст</param>
    private static async Task NotFound(Context? ctx)
    {
        var text = ctx?.PhrasesManager["command_not_found"];

        if (ctx == null || string.IsNullOrWhiteSpace(text))
            return;

        if (ctx is { Type: TypeEvents.Callback, Callback: not null })
            await ctx.Edit(text);
        else
            await ctx.Send(text);
    }

    /// <summary>
    /// Проверяет предусловия для команды.
    /// </summary>
    /// <param name="command">Команда</param>
    /// <param="ctx">Контекст</param>
    /// <returns>Результат проверки предусловия</returns>
    private static async Task<PreconditionResult> CheckPreconditions(Command command, Context? ctx)
    {
        var attrs = command.GetType().GetCustomAttributes();

        foreach (var attr in attrs)
        {
            if (
                !attr.GetType().IsSubclassOf(typeof(PreconditionAttribute)) ||
                attr is not PreconditionAttribute preconditionAttribute
            )
                continue;

            var result = await preconditionAttribute.CheckPermissionsAsync(command, ctx);

            if (!result.Success)
                return result;
        }

        return new PreconditionResult(true, default);
    }

    /// <summary>
    /// Получает команду по имени, типу события и уровню доступа.
    /// </summary>
    /// <param name="nameCommand">Имя команды</param>
    /// <param name="type">Тип события</param>
    /// <param name="access">Уровень доступа</param>
    /// <returns>Команда или null</returns>
    private static Command? GetCommandByName(string nameCommand, TypeEvents type, long access)
    {
        if (Commands == null)
            return null;

        Command? saveCommand = null;

        foreach (var command in Commands)
        {
            if (command == null)
                continue;

            var attr = command.GetType().GetCustomAttributes();

            if (nameCommand == "default" && attr.HaveAttribute<DefaultClassAttribute>())
            {
                saveCommand = command;
                break;
            }

            if (!command._names.Contains(nameCommand) ||
                command._typeEvents == null ||
                !command._typeEvents.Contains(type) ||
                command._visibility == Visibility.Collapsed) continue;

            if (saveCommand == null || command._accesses.Contains(access))
                saveCommand = command;
        }

        return saveCommand;
    }
}
