using ViewTelegramBot.Utils.Managers;

namespace ViewTelegramBot.Bot.Phrase;

/// <summary>
/// Класс для управления языковыми фразами.
/// </summary>
public class PhrasesManager(string filename)
{
    /// <summary>
    /// Менеджер для хранения фраз.
    /// </summary>
    private readonly ManagerObject<Dictionary<string, string>>? _manager = new ManagerObjectFile<Dictionary<string, string>>(filename);

    /// <summary>
    /// Получает фразу по ключу.
    /// </summary>
    /// <param name="phrase">Ключ фразы</param>
    /// <returns>Текст фразы или пустая строка</returns>
    public string this[string phrase] => _manager?.Obj != null && _manager.Obj.TryGetValue(phrase, out var value) ? value : "";

    /// <summary>
    /// Загружает фразы из файла.
    /// </summary>
    public async Task Load() => await _manager?.Load();

    /// <summary>
    /// Сохраняет фразы в файл.
    /// </summary>
    public async Task Save() => await _manager?.Save();

    /// <summary>
    /// Заменяет плейсхолдеры вида {{key}} на значения из словаря.
    /// </summary>
    /// <param name="val">Строка с плейсхолдерами</param>
    /// <returns>Строка с заменёнными плейсхолдерами</returns>
    public string Replace(string val)
    {
        if (_manager?.Obj == default)
            return val;

        var right = 0;
        while (val.Contains("{{") && val.Contains("}}"))
        {
            var l = val.IndexOf("{{", StringComparison.Ordinal);
            var r = val.IndexOf("}}", right, StringComparison.Ordinal);

            if (r == -1)
                break;

            if (r < l)
            {
                right = r + 2;
                continue;
            }

            right = 0;

            var sub = val[(l + 2)..r];

            if (_manager.Obj.TryGetValue(sub, out var value))
                val = val.Replace("{{" + sub + "}}", value);
            else
                right = r + 2;
        }

        return val;
    }

    /// <summary>
    /// Вставляет значения в строку-фразу по ключу.
    /// </summary>
    /// <param name="phrase">Ключ фразы</param>
    /// <param name="mass">Массив значений</param>
    /// <returns>Строка с подставленными значениями</returns>
    public string Insert(string phrase, params string[] mass) => Insert(phrase, mass.ToArray<object>());

    /// <summary>
    /// Вставляет значения в строку-фразу по ключу.
    /// </summary>
    /// <param name="phrase">Ключ фразы</param>
    /// <param name="mass">Коллекция значений</param>
    /// <returns>Строка с подставленными значениями</returns>
    public string Insert(string phrase, IEnumerable<string>? mass) => mass == null ? phrase : Insert(phrase, mass.ToArray<object>());

    /// <summary>
    /// Вставляет значения в строку-фразу по ключу (внутренний метод).
    /// </summary>
    /// <param name="phrase">Ключ фразы</param>
    /// <param name="mass">Массив значений</param>
    /// <returns>Строка с подставленными значениями</returns>
    private string Insert(string phrase, params object[] mass) => _manager?.Obj != null && _manager.Obj.TryGetValue(phrase, out var value) ? string.Format(value, mass) : "";
}
