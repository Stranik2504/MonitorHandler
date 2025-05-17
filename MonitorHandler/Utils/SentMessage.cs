namespace MonitorHandler.Utils;

/// <summary>
/// Класс для исходящих сообщений WebSocket.
/// </summary>
public class SentMessage
{
    /// <summary>
    /// Тип исходящего сообщения.
    /// </summary>
    public TypeSentMessage Type { get; set; }

    /// <summary>
    /// Данные сообщения.
    /// </summary>
    public string Data { get; set; }
}
