namespace MonitorHandler.Models;

/// <summary>
/// Модель Docker-контейнера для хранения информации о контейнере.
/// </summary>
public class DockerContainer
{
    /// <summary>
    /// Уникальный идентификатор контейнера.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя контейнера.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Идентификатор связанного Docker-образа.
    /// </summary>
    public int ImageId { get; set; }

    /// <summary>
    /// Хэш связанного Docker-образа.
    /// </summary>
    public string ImageHash { get; set; }

    /// <summary>
    /// Текущий статус контейнера.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Ресурсы, выделенные контейнеру (опционально).
    /// </summary>
    public string? Resources { get; set; }

    /// <summary>
    /// Уникальный хэш контейнера.
    /// </summary>
    public string Hash { get; set; }
}
