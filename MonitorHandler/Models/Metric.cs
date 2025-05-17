namespace MonitorHandler.Models;

/// <summary>
/// Модель метрик сервера (CPU, RAM, диски, сеть и время).
/// </summary>
public class Metric
{
    /// <summary>
    /// Список загрузки каждого ядра процессора.
    /// </summary>
    public List<double> Cpus { get; set; }

    /// <summary>
    /// Используемая оперативная память (в байтах).
    /// </summary>
    public ulong UseRam { get; set; }

    /// <summary>
    /// Общий объём оперативной памяти (в байтах).
    /// </summary>
    public ulong TotalRam { get; set; }

    /// <summary>
    /// Используемое место на каждом диске (в байтах).
    /// </summary>
    public List<ulong> UseDisks { get; set; }

    /// <summary>
    /// Общий объём каждого диска (в байтах).
    /// </summary>
    public List<ulong> TotalDisks { get; set; }

    /// <summary>
    /// Количество отправленных данных по сети (в байтах).
    /// </summary>
    public ulong NetworkSend { get; set; }

    /// <summary>
    /// Количество полученных данных по сети (в байтах).
    /// </summary>
    public ulong NetworkReceive { get; set; }

    /// <summary>
    /// Временная метка сбора метрик.
    /// </summary>
    public DateTime Time { get; set; }
}
