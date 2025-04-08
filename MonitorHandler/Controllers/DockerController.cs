using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/server/docker")]
public class DockerController(
    ILogger<DockerController> logger,
    ServerManager manager
) : Controller
{
    private readonly ILogger<DockerController> _logger = logger;
    private readonly ServerManager _manager = manager;

    [HttpGet("containers")]
    public async Task<ActionResult<List<DockerContainer>>> GetContainers(
        [FromHeader] int userId,
        [FromHeader] string token,
        int serverId
    )
    {
        _logger.LogInformation("[DockerController]: GetContainers start");

        List<DockerContainer> containers;

        try
        {
            containers = await _manager.GetAllContainers(userId, token, serverId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: GetContainers end and return result");
        return containers;
    }

    [HttpGet("images")]
    public async Task<ActionResult<List<DockerImage>>> GetImages(
        [FromHeader]  int userId,
        [FromHeader] string token,
        int serverId
    )
    {
        _logger.LogInformation("[DockerController]: GetImages start");

        List<DockerImage> images;

        try
        {
            images = await _manager.GetAllImages(userId, token, serverId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: GetImages end and return result");
        return images;
    }

    [HttpPost("containers/start")]
    public async Task<ActionResult> StartContainer(
        [FromHeader]  int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int containerId
    )
    {
        _logger.LogInformation("[DockerController]: StartContainer start");

        bool result;

        try
        {
            result = await _manager.StartContainer(userId, token, serverId, containerId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: StartContainer end and return result");
        return Ok(result);
    }

    [HttpPost("containers/stop")]
    public async Task<ActionResult> StopContainer(
        [FromHeader]  int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int containerId
    )
    {
        _logger.LogInformation("[DockerController]: StopContainer start");

        bool result;

        try
        {
            result = await _manager.StopContainer(userId, token, serverId, containerId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: StopContainer end and return result");
        return Ok(result);
    }

    [HttpDelete("containers/remove")]
    public async Task<ActionResult> RemoveContainer(
        [FromHeader]  int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int containerId
    )
    {
        _logger.LogInformation("[DockerController]: RemoveContainer start");

        bool result;

        try
        {
            result = await _manager.RemoveContainer(userId, token, serverId, containerId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: RemoveContainer end and return result");
        return Ok(result);
    }

    [HttpDelete("image/remove")]
    public async Task<ActionResult> RemoveImage(
        [FromHeader]  int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int imageId
    )
    {
        _logger.LogInformation("[DockerController]: RemoveImage start");

        bool result;

        try
        {
            result = await _manager.RemoveImage(userId, token, serverId, imageId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[DockerController]: RemoveImage end and return result");
        return Ok(result);
    }
}