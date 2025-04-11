namespace MonitorHandler.Utils;

public static class Additions
{
    public static string GetString<T>(this IDictionary<T, string> dct, T val) where T : notnull => dct.TryGetValue(val, out var res) ? res : "";

    public static string GetString<T, J>(this IDictionary<T, J> dct, T val) where T : notnull =>
        dct.TryGetValue(val, out var res) && !string.IsNullOrWhiteSpace(res?.ToString()) ? res.ToString() : "";

    public static int GetInt<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => int.TryParse(dct.GetString(val), out var res) ? res : -1;

    public static double GetDouble<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => double.TryParse(dct.GetString(val), out var res) ? res : -1.0d;

    public static DateTime GetDateTime<T, J>(this IDictionary<T, J> dct, T val) where T : notnull => DateTime.TryParse(dct.GetString(val), out var res) ? res : DateTime.UtcNow;

    public static int ToInt(this string val) => int.TryParse(val, out var res) ? res : -1;

}