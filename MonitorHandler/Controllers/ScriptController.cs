using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/server/script")]
public class ScriptController(
    ILogger<ScriptController> logger,
    ServerManager manager
) : Controller
{
    private readonly ILogger<ScriptController> _logger = logger;
    private readonly ServerManager _manager = manager;

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