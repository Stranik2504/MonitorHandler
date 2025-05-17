namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания списка разрешённых доступов для класса.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class AccessesAttribute : Attribute
{
    /// <summary>
    /// Список разрешённых доступов.
    /// </summary>
    public long[] Accesses { get; }

    // public AccessesAttribute(params Access[] accesses) => Accesses = accesses.Select(x => (long)x).ToArray();
}
