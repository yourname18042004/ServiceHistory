using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private const int BufferSize = 1024;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Khởi tạo WebSocket server
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add("http://localhost:5000/ws/");
        httpListener.Start();
        _logger.LogInformation("WebSocket Server started at ws://localhost:5000/ws/");

        // Đảm bảo dịch vụ dừng khi có yêu cầu dừng
        stoppingToken.Register(() => httpListener.Stop());

        while (!stoppingToken.IsCancellationRequested)
        {
            HttpListenerContext context = await httpListener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                await HandleConnectionAsync(webSocketContext.WebSocket, stoppingToken);
            }
        }
    }

    private async Task HandleConnectionAsync(WebSocket webSocket, CancellationToken stoppingToken)
    {
        byte[] buffer = new byte[BufferSize];

        // Tạo Event Log nếu chưa tồn tại
        if (!EventLog.SourceExists("HistoryWeb"))
        {
            EventLog.CreateEventSource("HistoryWeb", "Application");
        }

        using EventLog eventLog = new EventLog("Application")
        {
            Source = "HistoryWeb"
        };

        // Nhận và xử lý tin nhắn
        while (webSocket.State == WebSocketState.Open && !stoppingToken.IsCancellationRequested)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                _logger.LogInformation("Received message: {message}", message);

                // Ghi tin nhắn vào Event Log của Windows
                eventLog.WriteEntry($"Received WebSocket message: {message}", EventLogEntryType.Information);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", stoppingToken);
            }
        }
    }
}

