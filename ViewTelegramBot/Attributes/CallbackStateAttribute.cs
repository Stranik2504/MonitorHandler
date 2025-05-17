namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для пометки метода обработчиком определённого callback-состояния.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class CallbackStateAttribute(string state) : Attribute
{
    /// <summary>
    /// Имя состояния callback.
    /// </summary>
    public string State { get; } = state;
}
