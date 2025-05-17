using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

/// <summary>
/// Контроллер для получения и установки метрик сервера.
/// </summary>
[ApiController]
[Route("/api/v1/server/metrics")]
public class MetricsController(
    ILogger<MetricsController> logger,
    ServerManager manager
) : Controller
{
    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера Metrics.
    /// </summary>
    private readonly ILogger<MetricsController> _logger = logger;

    /// <summary>
    /// Менеджер серверов для работы с метриками.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Получает последние метрики сервера.
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Последние метрики сервера</returns>
    [HttpGet]
    public async Task<ActionResult<Metric>> GetMetrics(
        [FromHeader] int userId,
        [FromHeader] string token,
        int serverId
    )
    {
        _logger.LogInformation("[MetricsController]: GetMetrics start");

        Metric metrics;

        try
        {
            metrics = await _manager.GetLastMetric(userId, token, serverId);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[MetricsController]: GetMetrics end and return result");
        return metrics;
    }

    /// <summary>
    /// Устанавливает метрики сервера (альтернатива WebSocket).
    /// </summary>
    /// <param name="token">Токен авторизации</param>
    /// <param name="serverId">ID сервера</param>
    /// <param name="metric">Объект метрик</param>
    /// <returns>Результат выполнения операции</returns>
    [HttpPost]
    public async Task<ActionResult> SetMetrics(
        [FromHeader] string token,
        [FromHeader] int serverId,
        [FromBody] Metric metric
    )
    {
        _logger.LogInformation("[MetricsController]: SetMetrics start");

        bool result;

        try
        {
            result = await _manager.SetMetric(token, serverId, metric);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }

        _logger.LogInformation("[MetricsController]: SetMetrics end and return result");
        return Ok(result);
    }
}
