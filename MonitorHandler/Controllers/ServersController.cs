using System.Text.Json.Serialization;
using Database;
using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

/// <summary>
/// Контроллер для управления серверами пользователя.
/// </summary>
[ApiController]
[Route("/api/v1/servers")]
public class ServersController(
    ILogger<ServersController> logger,
    ServerManager manager
) : Controller
{
    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера Servers.
    /// </summary>
    private readonly ILogger<ServersController> _logger = logger;

    /// <summary>
    /// Менеджер серверов для выполнения операций с серверами.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Получает список всех серверов пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <returns>Список серверов</returns>
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

    /// <summary>
    /// Создаёт новый сервер для пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userToken">Токен пользователя</param>
    /// <param name="serverInfo">Информация о сервере</param>
    /// <returns>Токен сервера</returns>
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

    /// <summary>
    /// Добавляет существующий сервер по IP и токену.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userToken">Токен пользователя</param>
    /// <param name="ip">IP сервера</param>
    /// <param name="token">Токен сервера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Обновляет информацию о сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userToken">Токен пользователя</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="server">Обновлённый объект сервера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Удаляет сервер пользователя.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userToken">Токен пользователя</param>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Результат выполнения операции</returns>
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

    /// <summary>
    /// Получает токен сервера по его идентификатору.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="userToken">Токен пользователя</param>
    /// <param name="id">ID сервера</param>
    /// <returns>Токен сервера</returns>
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

/// <summary>
/// Класс для передачи информации о сервере при создании.
/// </summary>
public class ServerInfo
{
    /// <summary>
    /// Имя сервера.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// IP-адрес сервера.
    /// </summary>
    [JsonPropertyName("ip")]
    public string Ip { get; set; }
}
