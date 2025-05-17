namespace MonitorHandler.Utils;

/// <summary>
/// Сервис фоновой очистки метрик сервера.
/// </summary>
public class MetricsCleanupService(
    IServiceProvider serviceProvider,
    ILogger<MetricsCleanupService> logger,
    ServerManager manager
)
    : BackgroundService
{
    /// <summary>
    /// Провайдер сервисов для получения зависимостей.
    /// </summary>
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Логгер для вывода информации и ошибок сервиса очистки метрик.
    /// </summary>
    private readonly ILogger<MetricsCleanupService> _logger = logger;

    /// <summary>
    /// Менеджер серверов для выполнения операций очистки метрик.
    /// </summary>
    private readonly ServerManager _manager = manager;

    /// <summary>
    /// Основной цикл фоновой задачи по очистке метрик.
    /// </summary>
    /// <param name="stoppingToken">Токен отмены задачи</param>
    /// <returns>Задача выполнения</returns>
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
                var result = await _manager.ClearMetrics();
                _logger.LogInformation($"Очистка метрик завершена. Удалено {(result ? "успешно" : "не успешно")}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке метрик.");
            }
        }
    }
}
