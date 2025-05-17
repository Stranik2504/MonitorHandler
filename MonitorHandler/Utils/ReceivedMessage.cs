using MonitorHandler.Models;

namespace MonitorHandler.Utils;

/// <summary>
/// Базовый класс для входящих сообщений WebSocket.
/// </summary>
public class ReceivedMessage
{
    /// <summary>
    /// Тип входящего сообщения.
    /// </summary>
    public TypeReceivedMessage Type { get; set; }

    /// <summary>
    /// Данные сообщения.
    /// </summary>
    public string? Data { get; set; }
}

/// <summary>
/// Класс для стартового сообщения WebSocket, содержащего токен, метрики и docker-объекты.
/// </summary>
public class ReceivedStartMessage : ReceivedMessage
{
    /// <summary>
    /// Токен сервера.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Метрики сервера.
    /// </summary>
    public Metric? Metric { get; set; }

    /// <summary>
    /// Список docker-образов.
    /// </summary>
    public List<DockerImage>? DockerImages { get; set; }

    /// <summary>
    /// Список docker-контейнеров.
    /// </summary>
    public List<DockerContainer>? DockerContainers { get; set; }
}
