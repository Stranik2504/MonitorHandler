using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для задания уровня видимости класса.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class VisibilityAttribute(Visibility visibility) : Attribute
{
    /// <summary>
    /// Уровень видимости.
    /// </summary>
    public Visibility Visibility { get; } = visibility;
}
