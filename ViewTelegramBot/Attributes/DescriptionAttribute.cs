namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class DescriptionAttribute(string? description) : Attribute
{
    public string? Description { get; } = description;
}