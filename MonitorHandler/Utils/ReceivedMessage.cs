using MonitorHandler.Models;

namespace MonitorHandler.Utils;

public class ReceivedMessage
{
    public TypeReceivedMessage Type { get; set; }
    public string Data { get; set; }
}