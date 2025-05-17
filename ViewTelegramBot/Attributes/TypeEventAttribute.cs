using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания типа события для класса.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TypeEventAttribute(params TypeEvents[]? typeEvents) : Attribute
{
    /// <summary>
    /// Массив типов событий.
    /// </summary>
    public TypeEvents[]? TypeEvents  { get; } = typeEvents;
}
