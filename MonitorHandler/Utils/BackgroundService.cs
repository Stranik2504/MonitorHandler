namespace MonitorHandler.Utils;

public class MetricsCleanupService(
    IServiceProvider serviceProvider,
    ILogger<MetricsCleanupService> logger,
    ServerManager manager
)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextRun = now.Date.AddDays(1); // Следующая полуночь
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            try
            {
                var result = await manager.ClearMetrics();
                logger.LogInformation($"Очистка метрик завершена. Удалено {(result ? "успешно" : "не успешно")}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при очистке метрик.");
            }
        }
    }
}