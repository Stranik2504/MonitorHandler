using System.Reflection;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Telegram.Bot.Types.ReplyMarkups;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;

namespace ViewTelegramBot.Utils;

public static class Additions
{
    private static ILogger? _logger;
    private static readonly Mutex Mutex = new();

    public static void CreateLogger() =>
        _logger = new LoggerConfiguration()
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.File(Program.ErrorFile, LogEventLevel.Error)
            .CreateLogger();

    public static void Log<T>(this T obj)
    {
        Mutex.WaitOne();
        _logger?.Error("{obj}", obj);
        Mutex.ReleaseMutex();
    }

    public static void Log(this Exception ex) => ex.Log<Exception>();

    public static void LogInfo(this string text)
    {
        Mutex.WaitOne();
        _logger?.Information("{text}", text);
        Mutex.ReleaseMutex();
    }

    public static ILogger? GetLogger() => _logger;

    public static void ForEach<T>(this IEnumerable<T> en, Action<T> action)
    {
        foreach (var item in en)
        {
            action?.Invoke(item);
        }
    }

    public static string ConvertToMark2(this string input)
    {
        const string specials = "-.()<>`#+-={}!";
        var result = (string)input.Clone();

        var r = specials.Select(x => x.ToString()).Aggregate(result,
            (current, item) => current.Replace(item, string.Join(@"\", (" " + item).ToList())[1..]));

        return r;
    }

    public static Document? Convert(this Telegram.Bot.Types.Document document)
    {
        if (string.IsNullOrWhiteSpace(document?.FileId) || string.IsNullOrWhiteSpace(document?.FileName))
            return null;

        return new Document(document.FileName, document.FileId, document.FileSize);
    }

    public static List<T> ToList<T>(this T obj) where T : Document?
    {
        if (obj == null)
            return [];

        return [obj];
    }

    public static InlineKeyboardMarkup? Generate(this Keyboard? keyboard)
    {
        if (keyboard == null || keyboard.Lines.Count == 0)
            return null;

        var lst = keyboard.Lines.Select(line =>
            line.Buttons.Select(button =>
                InlineKeyboardButton.WithCallbackData(button.Text, button.Payload ?? string.Empty)
            ).ToList()
        ).ToList();

        return new InlineKeyboardMarkup(lst);
    }

    public static Keyboard ReGenerate(this InlineKeyboardMarkup? markup)
    {
        Keyboard keyboard = new(true);

        if (markup == null)
            return keyboard;

        markup.InlineKeyboard.ForEach(line =>
        {
            keyboard.AddLine(new Line());

            line.ForEach(button =>
            {
                if (button.CallbackData != null)
                    keyboard.Lines[^1].AddButton(new Button(button.Text, button.CallbackData));
            });
        });

        return keyboard;
    }

    public static bool HaveAttribute<T>(this IEnumerable<object> objs) => objs.Any(item => item.GetType() == typeof(T));

    public static bool HaveParam<T>(this IEnumerable<ParameterInfo> lst) => lst.Any(x => x.ParameterType == typeof(T));

    private static bool HaveState(this IEnumerable<Attribute> objs, string state) => objs.Any(item =>
    {
        if (item is StateAttribute stateAtt)
            return stateAtt.State == state;

        return false;
    });

    private static bool HaveCallbackState(this IEnumerable<Attribute> objs, string state) => objs.Any(item =>
    {
        if (item is CallbackStateAttribute callbackState)
            return callbackState.State == state;

        return false;
    });

    public static MethodInfo? GetByState(this IEnumerable<MethodInfo> methods, string state, TypeEvents type)
    {
        foreach (var method in methods)
        {
            var attr = method.GetCustomAttributes().ToList();

            if (type == TypeEvents.Text && state == "default" && attr.HaveAttribute<DefaultStateAttribute>())
                return method;

            if (type == TypeEvents.Text && attr.HaveState(state))
                return method;

            if (type == TypeEvents.Callback && attr.HaveCallbackState(state))
                return method;
        }

        return null;
    }

    public static string GetString<T>(this IDictionary<T, string> dct, T val) where T : notnull => dct.TryGetValue(val, out var res) ? res : "";

    public static string GetString<T, J>(this IDictionary<T, J> dct, T val) where T : notnull =>
        dct.TryGetValue(val, out var res) && !string.IsNullOrWhiteSpace(res?.ToString()) ? res.ToString() : "";

    public static int GetInt<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => int.TryParse(dct.GetString(val), out var res) ? res : -1;

    public static double GetDouble<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => double.TryParse(dct.GetString(val), out var res) ? res : -1.0d;

    public static DateTime GetDateTime<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => DateTime.TryParse(dct.GetString(val), out var res) ? res : DateTime.UtcNow;

    public static int ToInt(this string val) => int.TryParse(val, out var res) ? res : -1;

    public static async Task<TValue?> First<TValue>(this IAsyncEnumerable<TValue?> en, Func<TValue?, bool> func)
    {
        await foreach (var item in en)
        {
            if (func?.Invoke(item) ?? false)
                return item;
        }

        return default;
    }
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; }
}