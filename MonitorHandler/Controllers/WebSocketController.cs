using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Database;
using MonitorHandler.Models;
using MonitorHandler.Utils;
using Newtonsoft.Json;

namespace MonitorHandler.Controllers;

/// <summary>
/// Контроллер для обработки WebSocket-соединения с сервером.
/// </summary>
public class WebSocketController (
    ILogger<WebSocketController> logger,
    IDatabase db,
    WebSocket webSocket,
    Config config
)
{
    /// <summary>
    /// Сообщение-ответ по умолчанию (OK).
    /// </summary>
    private readonly SentMessage OkMessage = new SentMessage() { Type = TypeSentMessage.Ok, Data = string.Empty };

    /// <summary>
    /// WebSocket-соединение.
    /// </summary>
    private readonly WebSocket _webSocket = webSocket;

    /// <summary>
    /// Логгер для вывода информации и ошибок контроллера WebSocket.
    /// </summary>
    private readonly ILogger<WebSocketController> _logger = logger;

    /// <summary>
    /// Интерфейс работы с базой данных.
    /// </summary>
    private readonly IDatabase _db = db;

    /// <summary>
    /// Конфигурация приложения.
    /// </summary>
    private readonly Config _config = config;

    /// <summary>
    /// Токен отмены для асинхронных операций.
    /// </summary>
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    /// <summary>
    /// Идентификатор сервера, связанного с этим WebSocket.
    /// </summary>
    private int _serverId;

    /// <summary>
    /// Флаг, указывающий, был ли сервер перезапущен.
    /// </summary>
    private bool _isRestarted = false;

    /// <summary>
    /// Очередь сообщений, ожидающих отправки клиенту.
    /// </summary>
    private readonly ConcurrentQueue<SentMessage> _messages = new();

    /// <summary>
    /// TaskCompletionSource для получения результата от клиента.
    /// </summary>
    private readonly TaskCompletionSource<string> _resultTcs = new();

    /// <summary>
    /// Словарь всех активных WebSocket-клиентов по serverId.
    /// </summary>
    private static readonly ConcurrentDictionary<int, WebSocketController> _webSocketClients = new();

    /// <summary>
    /// Основной цикл обработки WebSocket-соединения.
    /// </summary>
    public async Task Run()
    {
        var buffer = new byte[1024 * 16]; // Можно уменьшить размер, т.к. будем собирать в поток

        while (true)
        {
            using var ms = new MemoryStream();
            WebSocketReceiveResult? result;
            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
                ms.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage && !result.CloseStatus.HasValue);

            if (result.CloseStatus.HasValue)
                break;

            ms.Seek(0, SeekOrigin.Begin);
            var receivedMessage = Encoding.UTF8.GetString(ms.ToArray());
            _logger.LogInformation("[WebSocketController]: Received message: {Message}", receivedMessage);

            var message = JsonConvert.DeserializeObject<ReceivedMessage>(receivedMessage);

            if (message == null)
            {
                await Send(OkMessage, result.MessageType, result.EndOfMessage);
                continue;
            }

            // ------------------[Обработка входных данных.]------------------

            if (message.Type == TypeReceivedMessage.Start)
            {
                var startMessage = JsonConvert.DeserializeObject<ReceivedStartMessage>(receivedMessage);

                if (startMessage == null)
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                var serverId = await GetServerId(startMessage.Token);
                _serverId = serverId;

                await AddMetric(startMessage.Metric);

                await DeleteAllByServerId("images");
                await DeleteAllByServerId("containers");
                await DeleteDockerFull();

                await SetDocker();
                await SetDockerImages(startMessage.DockerImages);
                await SetDockerContainers(startMessage.DockerContainers);

                _webSocketClients.AddOrUpdate(
                    serverId,
                    this,
                    (key, oldValue) => this
                );
                await SetStatus("online");
            }

            if (message.Type == TypeReceivedMessage.Metric)
            {
                var metric = JsonConvert.DeserializeObject<Metric>(message.Data ?? "");

                if (metric == null)
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await AddMetric(metric);
            }

            if (message.Type == TypeReceivedMessage.AddedDockerImage)
            {
                var image = JsonConvert.DeserializeObject<DockerImage>(message.Data ?? "");

                if (image == null)
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await AddDockerImageFull(image);
            }

            if (message.Type == TypeReceivedMessage.AddedDockerContainer)
            {
                var container = JsonConvert.DeserializeObject<DockerContainer>(message.Data ?? "");

                if (container == null)
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await AddDockerContainerFull(container);
            }

            if (message.Type == TypeReceivedMessage.RemovedDockerImage)
            {
                var imageHash = message.Data;

                if (string.IsNullOrWhiteSpace(imageHash))
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await RemoveDockerImage(imageHash);
            }

            if (message.Type == TypeReceivedMessage.RemovedDockerContainer)
            {
                var containerHash = message.Data;

                if (string.IsNullOrWhiteSpace(containerHash))
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await RemoveDockerContainer(containerHash);
            }

            if (message.Type == TypeReceivedMessage.UpdatedDockerContainer)
            {
                var container = JsonConvert.DeserializeObject<DockerContainer>(message.Data ?? "");

                if (container == null)
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                await UpdateDockerContainer(container);
            }

            if (message.Type == TypeReceivedMessage.Restarted)
            {
                _isRestarted = true;
            }

            if (message.Type == TypeReceivedMessage.Result)
            {
                var resultString = message.Data;

                if (string.IsNullOrWhiteSpace(resultString))
                {
                    await Send(OkMessage, result.MessageType, result.EndOfMessage);
                    continue;
                }

                _resultTcs.SetResult(resultString);
            }
            // ---------------------------------------------------------------

            var answer = NeededRequest() ?? OkMessage;

            await Send(answer, result.MessageType, result.EndOfMessage);
        }

        // Close connection
        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", _cancellationToken);
        await SetStatus(_isRestarted ? "restarted" : "offline");
        _logger.LogInformation("[WebSocketController]: WebSocket closed");
    }

    /// <summary>
    /// Ожидает результат от клиента с таймаутом.
    /// </summary>
    /// <returns>Результат или пустая строка при таймауте</returns>
    public async Task<string> WaitResult()
    {
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_config.TimeWaitAnswer), _cancellationToken);
        var completedTask = await Task.WhenAny(_resultTcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
            return string.Empty;

        return await _resultTcs.Task;
    }

    /// <summary>
    /// Добавляет сообщение на запуск контейнера в очередь.
    /// </summary>
    /// <param name="containerId">ID контейнера</param>
    public void AddMessageStartContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.StartContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Добавляет сообщение на остановку контейнера в очередь.
    /// </summary>
    /// <param name="containerId">ID контейнера</param>
    public void AddMessageStopContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.StopContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Добавляет сообщение на удаление контейнера в очередь.
    /// </summary>
    /// <param name="containerId">ID контейнера</param>
    public void AddMessageRemoveContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RemoveContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Добавляет сообщение на удаление образа в очередь.
    /// </summary>
    /// <param name="imageId">ID образа</param>
    public void AddMessageRemoveImage(int imageId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RemoveImage,
            Data = imageId.ToString()
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Добавляет сообщение на запуск скрипта в очередь.
    /// </summary>
    /// <param name="script">Скрипт</param>
    public void AddMessageRunScript(string script)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RunScript,
            Data = script
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Добавляет сообщение на выполнение команды в очередь.
    /// </summary>
    /// <param name="command">Команда</param>
    public void AddMessageRunCommand(string command)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RunCommand,
            Data = command
        };

        _messages.Enqueue(message);
    }

    /// <summary>
    /// Возвращает следующее сообщение из очереди, если оно есть.
    /// </summary>
    /// <returns>Сообщение или null</returns>
    private SentMessage? NeededRequest()
    {
        if (_messages.IsEmpty)
            return null;

        return _messages.TryDequeue(out var message) ? message : null;
    }

    /// <summary>
    /// Отправляет сообщение клиенту по WebSocket.
    /// </summary>
    /// <param name="message">Сообщение</param>
    /// <param name="type">Тип сообщения WebSocket</param>
    /// <param name="endOfMessage">Конец сообщения</param>
    private async Task Send(SentMessage message, WebSocketMessageType type, bool endOfMessage)
    {
        var messageString = JsonConvert.SerializeObject(message);
        var messageBytes = Encoding.UTF8.GetBytes(messageString);

        await _webSocket.SendAsync(
            new ArraySegment<byte>(messageBytes, 0, messageBytes.Length),
            type,
            endOfMessage,
            _cancellationToken
        );
    }

    /// <summary>
    /// Получает идентификатор сервера по токену.
    /// </summary>
    /// <param name="token">Токен сервера</param>
    /// <returns>ID сервера</returns>
    private async Task<int> GetServerId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("Invalid server token");

        var record = await _db.GetRecord("servers", "token", token);

        if (string.IsNullOrWhiteSpace(record.Id))
            throw new Exception("Invalid server token");

        if (string.IsNullOrWhiteSpace(record.Fields.GetString("id")))
            throw new Exception("Invalid server id");

        return record.Fields.GetInt("id");
    }

    /// <summary>
    /// Добавляет метрику в базу данных.
    /// </summary>
    /// <param name="metric">Метрика</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> AddMetric(Metric? metric)
    {
        if (metric == null)
            throw new Exception("Invalid metric");

        var result = await _db.Create("metrics", new Dictionary<string, object?>()
        {
            { "server_id", _serverId },
            { "cpus", JsonConvert.SerializeObject(metric.Cpus) },
            { "use_ram", metric.UseRam },
            { "total_ram", metric.TotalRam },
            { "use_disks", JsonConvert.SerializeObject(metric.UseDisks) },
            { "total_disks", JsonConvert.SerializeObject(metric.TotalDisks) },
            { "network_send", metric.NetworkSend },
            { "network_receive", metric.NetworkReceive },
            { "time", metric.Time }
        });

        return result.Success;
    }

    /// <summary>
    /// Устанавливает статус сервера.
    /// </summary>
    /// <param name="status">Статус</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> SetStatus(string status)
    {
        var result = await _db.Update(
            "servers",
            _serverId.ToString(),
            new Dictionary<string, object>()
            {
                { "status", status }
            }
        );

        return result;
    }

    /// <summary>
    /// Полностью удаляет docker-записи для сервера.
    /// </summary>
    /// <returns>Успех операции</returns>
    private async Task<bool> DeleteDockerFull()
    {
        return await _db.DeleteByField("docker", "server_id", _serverId);
    }

    /// <summary>
    /// Удаляет все записи по serverId из указанной таблицы.
    /// </summary>
    /// <param name="tableName">Имя таблицы</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> DeleteAllByServerId(string tableName)
    {
        var record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return false;

        var result = true;

        if (tableName.Contains("containers"))
        {
            var containers = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("containers")) ?? [];

            foreach (var containerId in containers)
            {
                result &= await _db.Delete("containers", containerId.ToString());
            }
        }

        if (tableName.Contains("images"))
        {
            var images = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images")) ?? [];

            foreach (var imageId in images)
            {
                result &= await _db.Delete("images", imageId.ToString());
            }
        }

        return result;
    }

    /// <summary>
    /// Добавляет docker-образ в базу данных.
    /// </summary>
    /// <param name="image">Образ</param>
    /// <returns>Успех и ID образа</returns>
    private async Task<(bool Success, int Id)> AddDockerImage(DockerImage image)
    {
        var result = await _db.Create(
            "images",
            new Dictionary<string, object?>()
            {
                { "name", image.Name },
                { "size", image.Size },
                { "hash", image.Hash }
            }
        );

        return (result.Success, result.Id.ToInt());
    }

    /// <summary>
    /// Добавляет docker-образ и обновляет docker-запись сервера.
    /// </summary>
    /// <param name="image">Образ</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> AddDockerImageFull(DockerImage image)
    {
        var result = await _db.Create(
            "images",
            new Dictionary<string, object?>()
            {
                { "name", image.Name },
                { "size", image.Size },
                { "hash", image.Hash }
            }
        );

        if (!result.Success)
            return false;

        var record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return false;

        var images = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images")) ?? [];
        images.Add(result.Id.ToInt());

        var res = await _db.Update(
            "docker",
            record.Id,
            new Dictionary<string, object>()
            {
                { "images", JsonConvert.SerializeObject(images) }
            }
        );

        return res;
    }

    /// <summary>
    /// Добавляет docker-контейнер в базу данных.
    /// </summary>
    /// <param name="container">Контейнер</param>
    /// <returns>Успех и ID контейнера</returns>
    private async Task<(bool Success, int Id)> AddDockerContainer(DockerContainer container)
    {
        var record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return (false, -1);

        var imageRecord = await _db.GetRecord("images", "hash", container.ImageHash);

        if (string.IsNullOrWhiteSpace(imageRecord.Id))
            return (false, -1);

        var imageIds = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images")) ?? [];

        if (!imageIds.Contains(imageRecord.Id.ToInt()))
            return (false, -1);

        var result = await _db.Create(
            "containers",
            new Dictionary<string, object?>()
            {
                { "name", container.Name },
                { "image_id", imageRecord.Id.ToInt() },
                { "image_hash", container.ImageHash },
                { "status", container.Status },
                { "resources", container.Resources },
                { "hash", container.Hash }
            }
        );

        return (result.Success, result.Id.ToInt());
    }

    /// <summary>
    /// Добавляет docker-контейнер и обновляет docker-запись сервера.
    /// </summary>
    /// <param name="container">Контейнер</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> AddDockerContainerFull(DockerContainer container)
    {
        var record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id))
            return false;

        var imageRecord = await _db.GetRecord("images", "hash", container.ImageHash);

        if (string.IsNullOrWhiteSpace(imageRecord.Id))
            return false;

        var imageIds = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images")) ?? [];

        if (!imageIds.Contains(imageRecord.Id.ToInt()))
            return false;

        var result = await _db.Create(
            "containers",
            new Dictionary<string, object?>()
            {
                { "name", container.Name },
                { "image_id", imageRecord.Id.ToInt() },
                { "image_hash", container.ImageHash },
                { "status", container.Status },
                { "resources", container.Resources },
                { "hash", container.Hash }
            }
        );

        if (!result.Success)
            return false;

        var containers = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("containers")) ?? [];
        containers.Add(result.Id.ToInt());

        var res = await _db.Update(
            "docker",
            record.Id,
            new Dictionary<string, object>()
            {
                { "containers", JsonConvert.SerializeObject(containers) }
            }
        );

        return res;
    }

    /// <summary>
    /// Создаёт docker-запись для сервера.
    /// </summary>
    /// <returns>Успех операции</returns>
    private async Task<bool> SetDocker()
    {
        var res = await _db.Create(
            "docker",
            new Dictionary<string, object?>()
            {
                { "server_id", _serverId },
                { "images", JsonConvert.SerializeObject(new List<int>()) },
                { "containers", JsonConvert.SerializeObject(new List<int>()) }
            }
        );

        return res.Success;
    }

    /// <summary>
    /// Устанавливает список docker-образов для сервера.
    /// </summary>
    /// <param name="images">Список образов</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> SetDockerImages(List<DockerImage>? images)
    {
        if (images == null)
            return false;

        var lst = new List<int>();

        foreach (var image in images)
        {
            var res = await AddDockerImage(image);

            if (!res.Success)
                return false;

            lst.Add(res.Id);
        }

        var result = await _db.UpdateByField(
            "docker",
            "server_id",
            _serverId,
            new Dictionary<string, object>()
            {
                { "images", JsonConvert.SerializeObject(lst) }
            }
        );

        return result;
    }

    /// <summary>
    /// Устанавливает список docker-контейнеров для сервера.
    /// </summary>
    /// <param name="containers">Список контейнеров</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> SetDockerContainers(List<DockerContainer>? containers)
    {
        if (containers == null)
            return false;

        var lst = new List<int>();

        foreach (var container in containers)
        {
            var res = await AddDockerContainer(container);

            if (!res.Success)
                return false;

            lst.Add(res.Id);
        }

        var result = await _db.UpdateByField(
            "docker",
            "server_id",
            _serverId,
            new Dictionary<string, object>()
            {
                { "containers", JsonConvert.SerializeObject(lst) }
            }
        );

        return result;
    }

    /// <summary>
    /// Удаляет docker-образ по хэшу.
    /// </summary>
    /// <param name="hashImage">Хэш образа</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> RemoveDockerImage(string hashImage)
    {
        var record = await _db.GetRecord("images", "hash", hashImage);
        var imageId = record.Id;

        if (string.IsNullOrWhiteSpace(imageId))
            return false;

        await _db.Delete("images", imageId);

        record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id) || !record.Fields.ContainsKey("images"))
            return false;

        var images = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("images"));

        if (images == null)
            return false;

        images.Remove(imageId.ToInt());

        var result = await _db.Update(
            "docker",
            record.Id,
            new Dictionary<string, object>()
            {
                { "images", JsonConvert.SerializeObject(images) }
            }
        );

        return result;
    }

    /// <summary>
    /// Удаляет docker-контейнер по хэшу.
    /// </summary>
    /// <param name="hashContainer">Хэш контейнера</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> RemoveDockerContainer(string hashContainer)
    {
        var record = await _db.GetRecord("containers", "hash", hashContainer);
        var containerId = record.Id;

        if (string.IsNullOrWhiteSpace(containerId))
            return false;

        await _db.Delete("containers", containerId);

        record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id) || !record.Fields.ContainsKey("containers"))
            return false;

        var containers = JsonConvert.DeserializeObject<List<int>>(record.Fields.GetString("containers"));

        if (containers == null)
            return false;

        containers.Remove(containerId.ToInt());

        var result = await _db.Update(
            "docker",
            record.Id,
            new Dictionary<string, object>()
            {
                { "containers", JsonConvert.SerializeObject(containers) }
            }
        );

        return result;
    }

    /// <summary>
    /// Обновляет docker-контейнер по хэшу.
    /// </summary>
    /// <param name="container">Контейнер</param>
    /// <returns>Успех операции</returns>
    private async Task<bool> UpdateDockerContainer(DockerContainer container)
    {
        var dict = new Dictionary<string, object>()
        {
            { "status", container.Status }
        };

        if (!string.IsNullOrWhiteSpace(container.Resources))
            dict.Add("resources", container.Resources);

        return await _db.UpdateByField(
            "containers",
            "hash",
            container.Hash,
            dict
        );
    }

    /// <summary>
    /// Получает контроллер WebSocket по serverId.
    /// </summary>
    /// <param name="serverId">ID сервера</param>
    /// <returns>Контроллер или null</returns>
    public static WebSocketController? GetController(int serverId) => _webSocketClients.GetValueOrDefault(serverId);
}
