namespace ViewTelegramBot.Attributes;

public class NameAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}