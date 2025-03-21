using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/servers")]
public class ServersController(ILogger<ServersController> logger) : Controller
{
    private readonly ILogger<ServersController> _logger = logger;

    [HttpGet]
    public async Task<IEnumerable<Server>> GetServers()
    {
        _logger.LogInformation("[ServerController]: GetServers start");

        var servers = await GetAllServers();

        _logger.LogInformation("[ServerController]: GetServers end and return result");
        return servers;
    }

    private async Task<List<Server>> GetAllServers()
    {
        return new List<Server>
        {
            new Server { Name = "Server1", Ip = "127.0.0.1", Status = "Online" },
            new Server { Name = "Server2", Ip = "192.168.0.2", Status = "Offline" }
        };
    }
}