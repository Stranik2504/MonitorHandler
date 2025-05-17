namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания описания класса.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DescriptionAttribute(string? description) : Attribute
{
    /// <summary>
    /// Описание класса.
    /// </summary>
    public string? Description { get; } = description;
}
