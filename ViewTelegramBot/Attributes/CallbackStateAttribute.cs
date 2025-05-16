namespace ViewTelegramBot.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class CallbackStateAttribute(string state) : Attribute
{
    public string State { get; } = state;
}