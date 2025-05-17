namespace MonitorHandler.Models;

/// <summary>
/// Модель Docker-образа для хранения информации об образе.
/// </summary>
public class DockerImage
{
    /// <summary>
    /// Уникальный идентификатор образа.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Имя Docker-образа.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Размер Docker-образа в мегабайтах.
    /// </summary>
    public double Size { get; set; }

    /// <summary>
    /// Уникальный хэш Docker-образа.
    /// </summary>
    public string Hash { get; set; }
}
