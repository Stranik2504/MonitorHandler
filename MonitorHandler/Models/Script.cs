using System.ComponentModel.DataAnnotations;

namespace MonitorHandler.Models;

/// <summary>
/// Модель пользовательского скрипта для сервера.
/// </summary>
public class Script
{
    /// <summary>
    /// Уникальный идентификатор скрипта.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Текст скрипта.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Время запуска скрипта (если задано).
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Смещение времени выполнения скрипта (если задано).
    /// </summary>
    public DateTime? OffsetTime { get; set; }
}
