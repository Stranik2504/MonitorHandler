using MonitorHandler.Models;

namespace MonitorHandler.Utils;

public class ReceivedMessage
{
    public TypeReceivedMessage Type { get; set; }
    public string? Data { get; set; }
}

public class ReceivedStartMessage : ReceivedMessage
{
    public string? Token { get; set; }
    public Metric? Metric { get; set; }
    public List<DockerImage>? DockerImages { get; set; }
    public List<DockerContainer>? DockerContainers { get; set; }
}