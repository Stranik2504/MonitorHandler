namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NamesAttribute(params string[] names) : Attribute
{
    public IReadOnlyList<string> Names => names.ToList().AsReadOnly();
}