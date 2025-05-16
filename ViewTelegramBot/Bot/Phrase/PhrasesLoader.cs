using ViewTelegramBot.Utils.Managers;

using static ViewTelegramBot.Utils.Consts;

namespace ViewTelegramBot.Bot.Phrase;

public class PhrasesLoader
{
    private static readonly ManagerObject<Dictionary<string, string>>? _manager = default;
    private static readonly Dictionary<string, PhrasesManager> _phrasesManagers = new();

    public static PhrasesManager EmptyPhrasesManager => new PhrasesManager("");

    static PhrasesLoader() => _manager = new ManagerObjectFile<Dictionary<string, string>>(PhrasesInfoFile);

    public static async Task Load() => await _manager?.Load();
    public static async Task Save() => await _manager?.Save();

    public static void Clear() => _phrasesManagers.Clear();

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

    public static List<string> GetLangs() => _manager?.Obj?.Keys.Where(x => x != "default" && x != "null").ToList() ?? [];

    public static string GetLangName(string lang) => lang switch
    {
        "en" => "English",
        "ru" => "Русский",
        _ => "Unknown"
    };

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