namespace ViewTelegramBot.Attributes;

/// <summary>
/// Атрибут для пометки метода обработчиком определённого состояния.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class StateAttribute(string state) : Attribute
{
    /// <summary>
    /// Имя состояния.
    /// </summary>
    public string State { get; } = state;
}
