using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace MonitorHandler.Models;

public class Server
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Ip { get; set; }
    public string Status { get; set; }
}