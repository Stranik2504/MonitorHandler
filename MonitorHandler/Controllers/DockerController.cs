using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

/// <summary>
/// Контроллер для управления Docker-контейнерами и образами на сервере.
/// </summary>
[ApiController]
[Route("/api/v1/server/docker")]
public class DockerController(
    ILogger<DockerController> logger,
    ServerManager manager
) : Controller
{
    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера Docker.
    /// </summary>
    private readonly ILogger<DockerController> _logger = logger;

    /// <summary>
    /// Менеджер серверов для выполнения операций с Docker.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Получает список всех Docker-контейнеров на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Список контейнеров Docker</returns>
    [HttpGet("{serverId:int}/containers")]
    public async Task<ActionResult<List<DockerContainer>>> GetContainers(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromRoute] int serverId
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

    /// <summary>
    /// Получает список всех Docker-образов на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Список Docker-образов</returns>
    [HttpGet("{serverId:int}/images")]
    public async Task<ActionResult<List<DockerImage>>> GetImages(
        [FromHeader]  int userId,
        [FromHeader] string token,
        [FromRoute] int serverId
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

    /// <summary>
    /// Запускает указанный Docker-контейнер на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="containerId">ID контейнера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Останавливает указанный Docker-контейнер на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="containerId">ID контейнера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Удаляет указанный Docker-контейнер с сервера.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="containerId">ID контейнера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Удаляет указанный Docker-образ с сервера.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="imageId">ID образа</param>
    /// <returns>Результат выполнения операции</returns>
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
