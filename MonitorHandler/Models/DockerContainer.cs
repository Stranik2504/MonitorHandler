namespace MonitorHandler.Models;

public class DockerContainer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ImageId { get; set; }
    public string Status { get; set; }
    public string? Resources { get; set; }
}