using System.Net.WebSockets;
using System.Text;
using MonitorHandler.Utils;
using Newtonsoft.Json;

namespace MonitorHandler.Controllers;

public class WebSocketController (
    ILogger<WebSocketController> logger,
    WebSocket webSocket
)
{
    private readonly WebSocket _webSocket = webSocket;
    private readonly ILogger<WebSocketController> _logger = logger;
    private readonly CancellationToken _cancellationToken = CancellationToken.None;

    private readonly SentMessage OkMessage = new SentMessage() { Type = TypeSentMessage.Ok, Data = string.Empty };

    // TODO: work with this
    public async Task Run()
    {
        var buffer = new byte[1024 * 4];

        // Read message
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
                // TODO: get serverId from message
                // token, metrics, docker_images, docker_containers

                Program.WebSocketClients.Add(1, this);
            }

            // ---------------------------------------------------------------

            var answer = await NeededRequest() ?? OkMessage;

            await Send(answer, result.MessageType, result.EndOfMessage);

            // Read the next message
            result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);
        }

        // Close connection
        // TODO: mark server as offline
        await _webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, _cancellationToken);
        _logger.LogInformation("[WebSocketController]: WebSocket closed");
    }

    // TODO: Check list request to send and make request to it
    private async Task<SentMessage?> NeededRequest()
    {
        return null;
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
}