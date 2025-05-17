using ViewTelegramBot.Utils.Managers;

using static ViewTelegramBot.Utils.Consts;

namespace ViewTelegramBot.Bot.Phrase;

/// <summary>
/// Класс для загрузки и управления языковыми фразами.
/// </summary>
public class PhrasesLoader
{
    /// <summary>
    /// Менеджер для хранения соответствия языков и файлов с фразами.
    /// </summary>
    private static readonly ManagerObject<Dictionary<string, string>>? _manager = default;

    /// <summary>
    /// Кэш менеджеров фраз по языкам.
    /// </summary>
    private static readonly Dictionary<string, PhrasesManager> _phrasesManagers = new();

    /// <summary>
    /// Пустой менеджер фраз.
    /// </summary>
    public static PhrasesManager EmptyPhrasesManager => new PhrasesManager("");

    static PhrasesLoader() => _manager = new ManagerObjectFile<Dictionary<string, string>>(PhrasesInfoFile);

    /// <summary>
    /// Загружает словарь языков и файлов фраз.
    /// </summary>
    public static async Task Load() => await _manager?.Load();

    /// <summary>
    /// Сохраняет словарь языков и файлов фраз.
    /// </summary>
    public static async Task Save() => await _manager?.Save();

    /// <summary>
    /// Очищает кэш менеджеров фраз.
    /// </summary>
    public static void Clear() => _phrasesManagers.Clear();

    /// <summary>
    /// Загружает менеджер фраз для указанного языка.
    /// </summary>
    /// <param name="language">Язык</param>
    /// <returns>Менеджер фраз</returns>
    public static async Task<PhrasesManager> LoadPhrasesManager(string language)
    {
        language = language.ToLower();

        if (_phrasesManagers.TryGetValue(language, out var phrasesManager))
            return phrasesManager;

        if (_manager?.Obj == null)
            return await GetValueOrCreate("null", PhrasesFolder + "null.inf");

        if (_manager.Obj.TryGetValue(language, out var value))
            return await GetValueOrCreate(language, PhrasesFolder + value);

        if (_manager.Obj.TryGetValue("default", out value))
            return await GetValueOrCreate("default", PhrasesFolder + value);

        return await GetValueOrCreate("null", PhrasesFolder + "null.inf");
    }

    /// <summary>
    /// Возвращает список поддерживаемых языков.
    /// </summary>
    /// <returns>Список языков</returns>
    public static List<string> GetLangs() => _manager?.Obj?.Keys.Where(x => x != "default" && x != "null").ToList() ?? [];

    /// <summary>
    /// Возвращает название языка по коду.
    /// </summary>
    /// <param name="lang">Код языка</param>
    /// <returns>Название языка</returns>
    public static string GetLangName(string lang) => lang switch
    {
        "en" => "English",
        "ru" => "Русский",
        _ => "Unknown"
    };

    /// <summary>
    /// Получает менеджер фраз по языку или создаёт новый.
    /// </summary>
    /// <param name="language">Язык</param>
    /// <param name="filename">Файл с фразами</param>
    /// <returns>Менеджер фраз</returns>
    private static async Task<PhrasesManager> GetValueOrCreate(string language, string filename)
    {
        if (_phrasesManagers.TryGetValue(language, out var phrasesManager))
            return phrasesManager;

        phrasesManager = new PhrasesManager(filename);
        await phrasesManager.Load();

        _phrasesManagers.Add(language, phrasesManager);
        return phrasesManager;
    }
}
