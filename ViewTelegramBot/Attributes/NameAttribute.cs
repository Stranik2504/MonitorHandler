namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания имени класса или команды.
/// </summary>
public class NameAttribute(string name) : Attribute
{
    /// <summary>
    /// Имя.
    /// </summary>
    public string Name { get; } = name;
}
