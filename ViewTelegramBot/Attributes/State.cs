namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class StateAttribute(string state) : Attribute
{
    public string State { get; } = state;
}