namespace MonitorHandler.Utils;

/// <summary>
/// Типы входящих сообщений WebSocket от сервера.
/// </summary>
public enum TypeReceivedMessage
{
     /// <summary>
     /// Сообщение с метриками.
     /// </summary>
     Metric,
     /// <summary>
     /// Добавлен docker-образ.
     /// </summary>
     AddedDockerImage,
     /// <summary>
     /// Добавлен docker-контейнер.
     /// </summary>
     AddedDockerContainer,
     /// <summary>
     /// Удалён docker-образ.
     /// </summary>
     RemovedDockerImage,
     /// <summary>
     /// Удалён docker-контейнер.
     /// </summary>
     RemovedDockerContainer,
     /// <summary>
     /// Обновлён docker-контейнер.
     /// </summary>
     UpdatedDockerContainer,
     /// <summary>
     /// Запуск сервера.
     /// </summary>
     Start,
     /// <summary>
     /// Результат выполнения команды.
     /// </summary>
     Result,
     /// <summary>
     /// Сервер был перезапущен.
     /// </summary>
     Restarted,
     /// <summary>
     /// Нет типа (по умолчанию).
     /// </summary>
     None
}

/// <summary>
/// Типы исходящих сообщений WebSocket к серверу.
/// </summary>
public enum TypeSentMessage
{
     /// <summary>
     /// Запустить контейнер.
     /// </summary>
     StartContainer,
     /// <summary>
     /// Остановить контейнер.
     /// </summary>
     StopContainer,
     /// <summary>
     /// Удалить контейнер.
     /// </summary>
     RemoveContainer,
     /// <summary>
     /// Удалить образ.
     /// </summary>
     RemoveImage,
     /// <summary>
     /// Запустить скрипт.
     /// </summary>
     RunScript,
     /// <summary>
     /// Выполнить команду.
     /// </summary>
     RunCommand,
     /// <summary>
     /// Перезапустить сервер.
     /// </summary>
     Restart,
     /// <summary>
     /// Сообщение OK.
     /// </summary>
     Ok
}
