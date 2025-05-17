using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

/// <summary>
/// Главный контроллер API MonitorHandler.
/// </summary>
[ApiController]
[Route("/api/v1/")]
public class MainController(
    ILogger<MainController> logger,
    ServerManager manager
) : Controller
{
    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера Main.
    /// </summary>
    private readonly ILogger<MainController> _logger = logger;

    /// <summary>
    /// Менеджер серверов для выполнения операций с пользователями.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Возвращает строку-индикатор работы API.
    /// </summary>
    /// <returns>Строка "MonitorHandler API"</returns>
    [HttpGet]
    public ActionResult<string> Get()
    {
        return "MonitorHandler API";
    }

    /// <summary>
    /// Регистрирует нового пользователя по имени.
    /// </summary>
    /// <param name="userName">Имя пользователя</param>
    /// <returns>Зарегистрированный пользователь</returns>
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
