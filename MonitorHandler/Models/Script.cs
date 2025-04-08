using System.ComponentModel.DataAnnotations;

namespace MonitorHandler.Models;

public class Script
{
    [Key]
    public int Id { get; set; }
    public string Text { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? OffsetTime { get; set; }
}