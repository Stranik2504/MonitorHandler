namespace ViewTelegramBot.Utils;

public static class Additions
{
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
}