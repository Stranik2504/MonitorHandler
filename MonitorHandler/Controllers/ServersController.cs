using System.Text.Json.Serialization;
using Database;
using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/servers")]
public class ServersController(
    ILogger<ServersController> logger,
    ServerManager manager
) : Controller
{
    private readonly ILogger<ServersController> _logger = logger;
    private readonly ServerManager _manager = manager;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Server>>> GetServers(
        [FromHeader] int userId,
        [FromHeader] string token
    )
    {
        _logger.LogInformation("[ServerController]: GetServers start");
        List<Server> servers;

        try
        {
            servers = await _manager.GetAllServers(userId, token);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: GetServers end and return result");
        return servers;
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateServer(
        [FromHeader] int userId,
        [FromHeader] string userToken,
        [FromBody] ServerInfo serverInfo
    )
    {
        _logger.LogInformation("[ServerController]: CreateServer start");

        bool success;
        string token;

        try
        {
            (success, token) = await _manager.CreateServer(userId, userToken, serverInfo.Name, serverInfo.Ip);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: CreateServer end and return result");
        return success ? Ok(token) : Problem();
    }

    [HttpPost("add")]
    public async Task<ActionResult> AddServer(
        [FromHeader] int userId,
        [FromHeader] string userToken,
        [FromQuery] string ip,
        [FromQuery] string token
    )
    {
        _logger.LogInformation("[ServerController]: AddServer start");

        bool result;

        try
        {
            result = await _manager.AddServer(userId, userToken, ip, token);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: AddServer end and return result");
        return result ? Ok() : Problem();
    }

    [HttpPut]
    public async Task<ActionResult> UpdateServer(
        [FromHeader] int userId,
        [FromHeader] string userToken,
        [FromHeader] int serverId,
        [FromQuery] Server server
    )
    {
        _logger.LogInformation("[ServerController]: UpdateServer start");

        bool result;

        try
        {
            result = await _manager.UpdateServer(userId, userToken, serverId, server);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: UpdateServer end and return result");
        return result ? Ok() : Problem();
    }

    [HttpDelete]
    public async Task<ActionResult> DeleteServer(
        [FromHeader] int userId,
        [FromHeader] string userToken,
        [FromHeader] int serverId
    )
    {
        _logger.LogInformation("[ServerController]: DeleteServer start");
        bool result;

        try
        {
            result = await _manager.DeleteServer(userId, userToken, serverId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: DeleteServer end and return result");
        return result ? Ok() : Problem();
    }

    [HttpGet("token")]
    public async Task<ActionResult<string>> GetToken(
        [FromHeader] int userId,
        [FromHeader] string userToken,
        [FromQuery] int id
    )
    {
        _logger.LogInformation("[ServerController]: GetToken start");
        string token;

        try
        {
            token = await _manager.GetToken(userId, userToken, id);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ServerController]: GetToken end and return result");
        return token;
    }
}

public class ServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("ip")]
    public string Ip { get; set; }
}