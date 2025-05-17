namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания списка имён класса.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NamesAttribute(params string[] names) : Attribute
{
    /// <summary>
    /// Список имён.
    /// </summary>
    public IReadOnlyList<string> Names => names.ToList().AsReadOnly();
}
