using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class VisibilityAttribute(Visibility visibility) : Attribute
{
    public Visibility Visibility { get; } = visibility;
}