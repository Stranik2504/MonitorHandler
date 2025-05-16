using ViewTelegramBot.Utils;

namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TypeEventAttribute(params TypeEvents[]? typeEvents) : Attribute
{
    public TypeEvents[]? TypeEvents  { get; } = typeEvents;
}