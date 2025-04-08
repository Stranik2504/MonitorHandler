namespace MonitorHandler.Models;

public class Metric
{
    public int Cpu { get; set; }
    public int Ram { get; set; }
    public int Disk { get; set; }
    public int Network { get; set; }
    public DateTime Time { get; set; }
}