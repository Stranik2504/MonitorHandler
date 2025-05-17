using System.ComponentModel.DataAnnotations;

namespace MonitorHandler.Models;

/// <summary>
/// Модель сервера, принадлежащего пользователю.
/// </summary>
public class Server
{
    /// <summary>
    /// Уникальный идентификатор сервера.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Имя сервера.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// IP-адрес сервера.
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// Текущий статус сервера.
    /// </summary>
    public string Status { get; set; }
}
