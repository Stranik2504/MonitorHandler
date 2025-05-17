using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

/// <summary>
/// Контроллер для управления пользовательскими скриптами на сервере.
/// </summary>
[ApiController]
[Route("/api/v1/server/script")]
public class ScriptController(
    ILogger<ScriptController> logger,
    ServerManager manager
) : Controller
{
    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера Script.
    /// </summary>
    private readonly ILogger<ScriptController> _logger = logger;

    /// <summary>
    /// Менеджер серверов для выполнения операций со скриптами.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Получает список всех скриптов на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Список скриптов</returns>
    [HttpGet]
    public async Task<ActionResult<List<Script>>> GetScripts(
        [FromHeader] int userId,
        [FromHeader] string token,
        int serverId
    )
    {
        _logger.LogInformation("[ScriptController]: GetScripts start");

        List<Script> scripts;

        try
        {
            scripts = await _manager.GetAllScripts(userId, token, serverId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: GetScripts end and return result");
        return scripts;
    }

    /// <summary>
    /// Получает конкретный скрипт по его идентификатору.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="scriptId">ID скрипта</param>
    /// <returns>Скрипт</returns>
    [HttpGet("script")]
    public async Task<ActionResult<Script>> GetScript(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int scriptId
    )
    {
        _logger.LogInformation("[ScriptController]: GetScript start");

        Script script;

        try
        {
            script = await _manager.GetScript(userId, token, serverId, scriptId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: GetScript end and return result");
        return script;
    }

    /// <summary>
    /// Запускает указанный скрипт на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="scriptId">ID скрипта</param>
    /// <returns>Результат выполнения операции</returns>
    [HttpPost("run")]
    public async Task<ActionResult> RunScript(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int scriptId
    )
    {
        _logger.LogInformation("[ScriptController]: RunScript start");

        bool result;

        try
        {
            result = await _manager.RunScript(userId, token, serverId, scriptId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: RunScript end and return result");
        return Ok(result);
    }

    /// <summary>
    /// Создаёт новый скрипт на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="script">Объект скрипта</param>
    /// <returns>Результат выполнения операции</returns>
    [HttpPut]
    public async Task<ActionResult> CreateScript(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        [FromBody] Script script
    )
    {
        _logger.LogInformation("[ScriptController]: CreateScript start");

        bool result;

        try
        {
            result = await _manager.CreateScript(userId, token, serverId, script);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: CreateScript end and return result");
        return result ? Ok() : Problem();
    }

    /// <summary>
    /// Удаляет скрипт с сервера.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="scriptId">ID скрипта</param>
    /// <returns>Результат выполнения операции</returns>
    [HttpDelete]
    public async Task<ActionResult> DeleteScript(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        int scriptId
    )
    {
        _logger.LogInformation("[ScriptController]: DeleteScript start");

        bool result;

        try
        {
            result = await _manager.DeleteScript(userId, token, serverId, scriptId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: DeleteScript end and return result");
        return result ? Ok() : Problem();
    }

    /// <summary>
    /// Выполняет произвольную команду на сервере.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="command">Команда для выполнения</param>
    /// <returns>Результат выполнения операции</returns>
    [HttpPost("command")]
    public async Task<ActionResult> RunCommand(
        [FromHeader] int userId,
        [FromHeader] string token,
        [FromHeader] int serverId,
        [FromBody] string command
    )
    {
        _logger.LogInformation("[ScriptController]: RunCommand start");

        bool result;

        try
        {
            result = await _manager.RunCommand(userId, token, serverId, command);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[ScriptController]: RunCommand end and return result");
        return Ok(result);
    }
}
