using System.Reflection;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Telegram.Bot.Types.ReplyMarkups;
using ViewTelegramBot.Attributes;
using ViewTelegramBot.Bot.Contexts;
using ViewTelegramBot.Bot.KeyboardUtls;
using ViewTelegramBot.Bot.Phrase;

namespace ViewTelegramBot.Utils;

/// <summary>
/// Статический класс с методами-расширениями и вспомогательными методами для работы с ботом и коллекциями.
/// </summary>
public static class Additions
{
    /// <summary>
    /// Логгер для вывода информации и ошибок.
    /// </summary>
    private static ILogger? _logger;

    /// <summary>
    /// Мьютекс для синхронизации логирования.
    /// </summary>
    private static readonly Mutex Mutex = new();

    /// <summary>
    /// Создаёт глобальный логгер.
    /// </summary>
    public static void CreateLogger() =>
        _logger = new LoggerConfiguration()
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.File(Program.ErrorFile, LogEventLevel.Error)
            .CreateLogger();

    /// <summary>
    /// Логирует объект как ошибку.
    /// </summary>
    /// <typeparam name="T">Тип объекта</typeparam>
    /// <param name="obj">Объект</param>
    public static void Log<T>(this T obj)
    {
        Mutex.WaitOne();
        _logger?.Error("{obj}", obj);
        Mutex.ReleaseMutex();
    }

    /// <summary>
    /// Логирует исключение как ошибку.
    /// </summary>
    /// <param name="ex">Исключение</param>
    public static void Log(this Exception ex) => ex.Log<Exception>();

    /// <summary>
    /// Логирует информационное сообщение.
    /// </summary>
    /// <param name="text">Текст сообщения</param>
    public static void LogInfo(this string text)
    {
        Mutex.WaitOne();
        _logger?.Information("{text}", text);
        Mutex.ReleaseMutex();
    }

    /// <summary>
    /// Возвращает текущий логгер.
    /// </summary>
    /// <returns>Логгер</returns>
    public static ILogger? GetLogger() => _logger;

    /// <summary>
    /// Выполняет действие для каждого элемента коллекции.
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    /// <param name="en">Коллекция</param>
    /// <param name="action">Действие</param>
    public static void ForEach<T>(this IEnumerable<T> en, Action<T> action)
    {
        foreach (var item in en)
        {
            action?.Invoke(item);
        }
    }

    /// <summary>
    /// Экранирует специальные символы для MarkdownV2.
    /// </summary>
    /// <param name="input">Входная строка</param>
    /// <returns>Экранированная строка</returns>
    public static string ConvertToMark2(this string input)
    {
        const string specials = "-.()<>`#+-={}!";
        var result = (string)input.Clone();

        var r = specials.Select(x => x.ToString()).Aggregate(result,
            (current, item) => current.Replace(item, string.Join(@"\", (" " + item).ToList())[1..]));

        return r;
    }

    /// <summary>
    /// Преобразует Telegram-документ в объект Document.
    /// </summary>
    /// <param name="document">Документ Telegram</param>
    /// <returns>Document или null</returns>
    public static Document? Convert(this Telegram.Bot.Types.Document document)
    {
        if (string.IsNullOrWhiteSpace(document?.FileId) || string.IsNullOrWhiteSpace(document?.FileName))
            return null;

        return new Document(document.FileName, document.FileId, document.FileSize);
    }

    /// <summary>
    /// Преобразует объект Document в список.
    /// </summary>
    /// <typeparam name="T">Тип документа</typeparam>
    /// <param name="obj">Документ</param>
    /// <returns>Список документов</returns>
    public static List<T> ToList<T>(this T obj) where T : Document?
    {
        if (obj == null)
            return [];

        return [obj];
    }

    /// <summary>
    /// Генерирует InlineKeyboardMarkup из Keyboard.
    /// </summary>
    /// <param name="keyboard">Клавиатура</param>
    /// <returns>InlineKeyboardMarkup или null</returns>
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

    /// <summary>
    /// Преобразует InlineKeyboardMarkup обратно в Keyboard.
    /// </summary>
    /// <param name="markup">InlineKeyboardMarkup</param>
    /// <returns>Keyboard</returns>
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

    /// <summary>
    /// Проверяет наличие атрибута типа T в коллекции объектов.
    /// </summary>
    /// <typeparam name="T">Тип атрибута</typeparam>
    /// <param name="objs">Коллекция объектов</param>
    /// <returns>True, если атрибут найден</returns>
    public static bool HaveAttribute<T>(this IEnumerable<object> objs) => objs.Any(item => item.GetType() == typeof(T));

    /// <summary>
    /// Проверяет наличие параметра типа T в коллекции параметров.
    /// </summary>
    /// <typeparam name="T">Тип параметра</typeparam>
    /// <param name="lst">Коллекция параметров</param>
    /// <returns>True, если параметр найден</returns>
    public static bool HaveParam<T>(this IEnumerable<ParameterInfo> lst) => lst.Any(x => x.ParameterType == typeof(T));

    /// <summary>
    /// Проверяет наличие состояния в коллекции атрибутов.
    /// </summary>
    /// <param name="objs">Коллекция атрибутов</param>
    /// <param name="state">Имя состояния</param>
    /// <returns>True, если состояние найдено</returns>
    private static bool HaveState(this IEnumerable<Attribute> objs, string state) => objs.Any(item =>
    {
        if (item is StateAttribute stateAtt)
            return stateAtt.State == state;

        return false;
    });

    /// <summary>
    /// Проверяет наличие callback-состояния в коллекции атрибутов.
    /// </summary>
    /// <param name="objs">Коллекция атрибутов</param>
    /// <param name="state">Имя состояния</param>
    /// <returns>True, если callback-состояние найдено</returns>
    private static bool HaveCallbackState(this IEnumerable<Attribute> objs, string state) => objs.Any(item =>
    {
        if (item is CallbackStateAttribute callbackState)
            return callbackState.State == state;

        return false;
    });

    /// <summary>
    /// Получает метод по состоянию и типу события.
    /// </summary>
    /// <param name="methods">Коллекция методов</param>
    /// <param name="state">Имя состояния</param>
    /// <param name="type">Тип события</param>
    /// <returns>Метод или null</returns>
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

    /// <summary>
    /// Получает строковое значение по ключу из словаря типа IDictionary&lt;T, string&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Строковое значение или пустая строка</returns>
    public static string GetString<T>(this IDictionary<T, string> dct, T val) where T : notnull => dct.TryGetValue(val, out var res) ? res : "";

    /// <summary>
    /// Получает строковое представление значения по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Строковое значение или пустая строка</returns>
    public static string GetString<T, J>(this IDictionary<T, J> dct, T val) where T : notnull =>
        dct.TryGetValue(val, out var res) && !string.IsNullOrWhiteSpace(res?.ToString()) ? res.ToString() : "";

    /// <summary>
    /// Получает целое число по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Целое число или -1</returns>
    public static int GetInt<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => int.TryParse(dct.GetString(val), out var res) ? res : -1;

    /// <summary>
    /// Получает значение типа double по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Значение типа double или -1.0d</returns>
    public static double GetDouble<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => double.TryParse(dct.GetString(val), out var res) ? res : -1.0d;

    /// <summary>
    /// Получает значение типа DateTime по ключу из словаря типа IDictionary&lt;T, J&gt;.
    /// </summary>
    /// <typeparam name="T">Тип ключа</typeparam>
    /// <typeparam name="J">Тип значения</typeparam>
    /// <param name="dct">Словарь</param>
    /// <param name="val">Ключ</param>
    /// <returns>Значение типа DateTime или текущая дата и время (UTC)</returns>
    public static DateTime GetDateTime<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => DateTime.TryParse(dct.GetString(val), out var res) ? res : DateTime.UtcNow;

    /// <summary>
    /// Преобразует строку в целое число.
    /// </summary>
    /// <param name="val">Строка</param>
    /// <returns>Целое число или -1</returns>
    public static int ToInt(this string val) => int.TryParse(val, out var res) ? res : -1;

    /// <summary>
    /// Асинхронно возвращает первый элемент, удовлетворяющий условию.
    /// </summary>
    /// <typeparam name="TValue">Тип значения</typeparam>
    /// <param name="en">Асинхронная коллекция</param>
    /// <param name="func">Условие</param>
    /// <returns>Первый подходящий элемент или default</returns>
    public static async Task<TValue?> First<TValue>(this IAsyncEnumerable<TValue?> en, Func<TValue?, bool> func)
    {
        await foreach (var item in en)
        {
            if (func?.Invoke(item) ?? false)
                return item;
        }

        return default;
    }

    /// <summary>
    /// Возвращает максимальное значение из переданных.
    /// </summary>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <param name="values">Массив значений</param>
    /// <returns>Максимальное значение</returns>
    public static T Max<T>(params T[] values) where T : IComparable<T>
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty.");

        var maxValue = values[0];

        foreach (var value in values)
        {
            if (value.CompareTo(maxValue) > 0)
                maxValue = value;
        }
        return maxValue;
    }

    /// <summary>
    /// Преобразует количество байт в человекочитаемый формат.
    /// </summary>
    /// <param name="bytes">Количество байт</param>
    /// <param name="phrasesManager">Менеджер фраз</param>
    /// <returns>Строка с размером</returns>
    public static string ConvertToNormalViewBytes(this ulong bytes, PhrasesManager phrasesManager) => bytes switch
    {
        < 1024 => $"{bytes} {phrasesManager["bytes"]}",
        < 1024 * 1024 => $"{(double)bytes / 1024:F2} {phrasesManager["kilobytes"]}",
        < 1024 * 1024 * 1024 => $"{(double)bytes / (1024 * 1024):F2} {phrasesManager["megabytes"]}",
        _ => $"{(double)bytes / (1024 * 1024 * 1024):F2} {phrasesManager["gigabytes"]}"
    };
}

/// <summary>
/// Класс для передачи информации о сервере.
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Имя сервера.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// IP-адрес сервера.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; } = string.Empty;
}
