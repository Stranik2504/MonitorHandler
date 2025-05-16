using System.Reflection;
using System.Runtime.CompilerServices;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Commands;

public abstract class Command
{
    private const string CommandPrefix = "/";

    private TypeEvents[]? _typeEvents;
    private Visibility _visibility;
    private string? _description;
    private List<string> _names = [];
    private List<long> _accesses = [];

    protected string Name => string.IsNullOrWhiteSpace(_names[0]) ? "default" : _names[0];

    private static IReadOnlyCollection<Command?>? Commands { get; }

    static Command()
    {
        Commands = Assembly.GetAssembly(typeof(Command))?.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Command))).ToList()
            .Select(x => Activator.CreateInstance(x) as Command).ToList().AsReadOnly();
    }

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

    protected static async Task DefaultCommand(Context? ctx)
    {
        var command = Commands?.FirstOrDefault(x => x?.GetType().GetCustomAttributes().HaveAttribute<DefaultClassAttribute>() ?? false);
        var method = command?.GetType().GetMethods().FirstOrDefault(x => x.GetCustomAttributes().HaveAttribute<DefaultStateAttribute>());
        await ExecuteMethod(command, method, ctx);
    }

    protected static async Task DefaultCommand(Context? ctx, string state, TypeEvents type)
    {
        var command = Commands?.FirstOrDefault(x => x?.GetType().GetCustomAttributes().HaveAttribute<DefaultClassAttribute>() ?? false);
        var method = command?.GetType().GetMethods().GetByState(state, type);
        await ExecuteMethod(command, method, ctx);
    }

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

    private static async Task NotPredict(Context? ctx, string? message)
    {
        if (string.IsNullOrWhiteSpace(message) || ctx == null)
            return;

        if (ctx is { Type: TypeEvents.Callback, Callback: not null })
            await ctx.Edit(message);
        else
            await ctx.Send(message);
    }

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