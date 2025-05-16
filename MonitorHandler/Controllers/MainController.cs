using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/")]
public class MainController(
    ILogger<MainController> logger,
    ServerManager manager
) : Controller
{
    private readonly ILogger<MainController> _logger = logger;
    private readonly ServerManager _manager = manager;

    [HttpGet]
    public ActionResult<string> Get()
    {
        return "MonitorHandler API";
    }

    [HttpPost("register")]
    public async Task<ActionResult<User>> Register([FromBody] string userName)
    {
        _logger.LogInformation("[MainController]: GetMetrics start");

        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("[MainController]: UserName is null or empty");
            return BadRequest("UserName is null or empty");
        }

        var user = await _manager.AddUser(userName);

        _logger.LogInformation("[MainController]: GetMetrics end");
        return Ok(user);
    }
}