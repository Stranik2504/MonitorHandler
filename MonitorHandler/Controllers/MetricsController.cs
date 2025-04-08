using Microsoft.AspNetCore.Mvc;
using MonitorHandler.Models;
using MonitorHandler.Utils;

namespace MonitorHandler.Controllers;

[ApiController]
[Route("/api/v1/server/metrics")]
public class MetricsController(
    ILogger<MetricsController> logger,
    ServerManager manager
) : Controller
{
    private readonly ILogger<MetricsController> _logger = logger;
    private readonly ServerManager _manager = manager;

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


    // For set metrics instead of ws
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