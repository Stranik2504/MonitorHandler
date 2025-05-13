namespace MonitorHandler.Models;

public class Metric
{
    public List<double> Cpus { get; set; }
    public int UseRam { get; set; }
    public int TotalRam { get; set; }
    public List<int> UseDisks { get; set; }
    public List<int> TotalDisks { get; set; }
    public int NetworkSend { get; set; }
    public int NetworkReceive { get; set; }
    public DateTime Time { get; set; }
}