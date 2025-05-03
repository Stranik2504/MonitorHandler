using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using Database;
using MonitorHandler.Models;
using MonitorHandler.Utils;
using Newtonsoft.Json;

namespace MonitorHandler.Controllers;

public class WebSocketController (
    ILogger<WebSocketController> logger,
    IDatabase db,
    WebSocket webSocket,
    Config config
)
{
    private readonly SentMessage OkMessage = new SentMessage() { Type = TypeSentMessage.Ok, Data = string.Empty };

    private readonly WebSocket _webSocket = webSocket;
    private readonly ILogger<WebSocketController> _logger = logger;
    private readonly IDatabase _db = db;
    private readonly Config _config = config;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    private int _serverId;
    private bool _isRestarted = false;
    private readonly ConcurrentQueue<SentMessage> _messages = new();
    private readonly Mutex _mutex = new Mutex(false);
    private readonly TaskCompletionSource<string> _resultTcs = new TaskCompletionSource<string>();

    private static readonly ConcurrentDictionary<int, WebSocketController> _webSocketClients = new();

    public async Task Run()
    {
        var buffer = new byte[1024 * 4];

        // Read a message
        var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

        while (!result.CloseStatus.HasValue)
        {
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
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

            var answer = await NeededRequest() ?? OkMessage;

            await Send(answer, result.MessageType, result.EndOfMessage);

            // Read the next message
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
        }

        // Close connection
        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, _cancellationToken);
        await SetStatus(_isRestarted ? "restarted" : "offline");
        _logger.LogInformation("[WebSocketController]: WebSocket closed");
    }

    public async Task<string> WaitResult()
    {
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(_config.TimeWaitAnswer), _cancellationToken);
        var completedTask = await Task.WhenAny(_resultTcs.Task, timeoutTask);

        if (completedTask == timeoutTask)
            return string.Empty;

        return await _resultTcs.Task;
    }

    public void AddMessageStartContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.StartContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    public void AddMessageStopContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.StopContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    public void AddMessageRemoveContainer(int containerId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RemoveContainer,
            Data = containerId.ToString()
        };

        _messages.Enqueue(message);
    }

    public void AddMessageRemoveImage(int imageId)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RemoveImage,
            Data = imageId.ToString()
        };

        _messages.Enqueue(message);
    }

    public void AddMessageRunScript(string script)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RunScript,
            Data = script
        };

        _messages.Enqueue(message);
    }

    public void AddMessageRunCommand(string command)
    {
        var message = new SentMessage()
        {
            Type = TypeSentMessage.RunCommand,
            Data = command
        };

        _messages.Enqueue(message);
    }

    private async Task<SentMessage?> NeededRequest()
    {
        if (_messages.IsEmpty)
            return null;

        _mutex.WaitOne();

        return _messages.TryDequeue(out var message) ? message : null;
    }

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

    private async Task<bool> AddMetric(Metric? metric)
    {
        if (metric == null)
            throw new Exception("Invalid metric");

        var result = await _db.Create("metrics", new Dictionary<string, object>()
        {
            { "server_id", _serverId },
            { "cpu", metric.Cpu },
            { "ram", metric.Ram },
            { "disk", metric.Disk },
            { "network", metric.Network },
            { "time", metric.Time }
        });

        return result.Success;
    }

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

    private async Task<bool> DeleteAllByServerId(string tableName)
    {
        var result = await _db.DeleteByField(
            tableName,
            "server_id",
            _serverId
        );

        return result;
    }

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

    private async Task<(bool Success, int Id)> AddDockerContainer(DockerContainer container)
    {
        var result = await _db.Create(
            "containers",
            new Dictionary<string, object?>()
            {
                { "name", container.Name },
                { "image_id", container.ImageId },
                { "status", container.Status },
                { "resources", container.Resources },
                { "hash", container.Hash }
            }
        );

        return (result.Success, result.Id.ToInt());
    }

    private async Task<bool> AddDockerContainerFull(DockerContainer container)
    {
        var result = await _db.Create(
            "containers",
            new Dictionary<string, object?>()
            {
                { "name", container.Name },
                { "image_id", container.ImageId },
                { "status", container.Status },
                { "resources", container.Resources },
                { "hash", container.Hash }
            }
        );

        if (!result.Success)
            return false;

        var record = await _db.GetRecord("docker", "server_id", _serverId);

        if (string.IsNullOrWhiteSpace(record.Id))
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

    private async Task<bool> SetDockerImages(List<DockerImage>? images)
    {
        if (images == null)
            return false;

        if (!await DeleteAllByServerId("images"))
            return false;

        var lst = new List<int>();

        foreach (var image in images)
        {
            var res = await AddDockerImage(image);

            if (!res.Success)
                return false;

            lst.Add(res.Id);
        }

        await _db.UpdateByField(
            "docker",
            "server_id",
            _serverId,
            new Dictionary<string, object>()
            {
                { "images", JsonConvert.SerializeObject(lst) }
            }
        );

        return true;
    }

    private async Task<bool> SetDockerContainers(List<DockerContainer>? containers)
    {
        if (containers == null)
            return false;

        if (!await DeleteAllByServerId("containers"))
            return false;

        var lst = new List<int>();

        foreach (var container in containers)
        {
            var res = await AddDockerContainer(container);

            if (!res.Success)
                return false;

            lst.Add(res.Id);
        }

        await _db.UpdateByField(
            "docker",
            "server_id",
            _serverId,
            new Dictionary<string, object>()
            {
                { "containers", JsonConvert.SerializeObject(lst) }
            }
        );

        return true;
    }

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

    public static WebSocketController? GetController(int serverId) => _webSocketClients.GetValueOrDefault(serverId);
}