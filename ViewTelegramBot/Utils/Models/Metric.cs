namespace ViewTelegramBot.Utils.Models;

public class Metric
{
    public List<double> Cpus { get; set; }
    public ulong UseRam { get; set; }
    public ulong TotalRam { get; set; }
    public List<ulong> UseDisks { get; set; }
    public List<ulong> TotalDisks { get; set; }
    public ulong NetworkSend { get; set; }
    public ulong NetworkReceive { get; set; }
    public DateTime Time { get; set; }
}