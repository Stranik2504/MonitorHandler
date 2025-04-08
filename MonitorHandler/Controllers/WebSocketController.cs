using System.Net.WebSockets;
using System.Text;

namespace MonitorHandler.Controllers;

public class WebSocketController (
    ILogger<WebSocketController> logger,
    WebSocket webSocket
)
{
    private readonly WebSocket _webSocket = webSocket;
    private readonly ILogger<WebSocketController> _logger = logger;

    // TODO: work with this
    public async Task Run()
    {
        var buffer = new byte[1024 * 4];

        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            // Преобразуем полученное сообщение в строку
            var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Получено сообщение: {receivedMessage}");

            // Здесь можно добавить обработку полученной информации от агента,
            // например, сохранить метрики, информацию о Docker-контейнерах и т.д.

            // Пример: отправляем обратно подтверждение получения
            var responseMessage = Encoding.UTF8.GetBytes("Принято: " + receivedMessage);

            await webSocket.SendAsync(new ArraySegment<byte>(responseMessage, 0, responseMessage.Length),
                result.MessageType, result.EndOfMessage, CancellationToken.None);

            // Читаем следующее сообщение
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        // Close connection
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        _logger.LogInformation("[WebSocketController]: WebSocket closed");
    }
}