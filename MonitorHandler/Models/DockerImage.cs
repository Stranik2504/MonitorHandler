namespace MonitorHandler.Models;

public class DockerImage
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Size { get; set; }
    public string Hash { get; set; }
}