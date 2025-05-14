using ViewTelegramBot.Utils.Managers;

namespace ViewTelegramBot.Bot.Phrase;

public class PhrasesManager(string filename)
{
    private readonly ManagerObject<Dictionary<string, string>>? _manager = new ManagerObjectFile<Dictionary<string, string>>(filename);

    public string this[string phrase] => _manager?.Obj != null && _manager.Obj.TryGetValue(phrase, out var value) ? value : "";

    public async Task Load() => await _manager?.Load();
    public async Task Save() => await _manager?.Save();

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

    public string Insert(string phrase, params string[] mass) => Insert(phrase, mass.ToArray<object>());

    public string Insert(string phrase, IEnumerable<string>? mass) =>
        mass == null ? phrase : Insert(phrase, mass.ToArray<object>());

    private string Insert(string phrase, params object[] mass) =>
        _manager?.Obj != null && _manager.Obj.TryGetValue(phrase, out var value) ? string.Format(value, mass) : "";
}