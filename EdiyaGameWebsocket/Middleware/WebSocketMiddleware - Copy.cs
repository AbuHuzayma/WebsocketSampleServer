using EdiyaGameWebsocket.Data.Repository;
using EdiyaGameWebsocket.Entities;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EdiyaGameWebsocket.Middleware
{
    public class WebSocketMiddleware_
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly IPlayerRepository _playerRepository;
        public WebSocketMiddleware_(RequestDelegate next, IPlayerRepository playerRepository)
        {
            _next = next;
            _playerRepository = playerRepository;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await _next(context);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var socketId = Guid.NewGuid().ToString();

            _sockets.TryAdd(socketId, socket);

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024 * 4];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _sockets.TryRemove(socketId, out _);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var playerInfo = JsonSerializer.Deserialize<Player>(message);

                        // Update player's online status in database
                        playerInfo = await _playerRepository.GetPlayerAsync(playerInfo.Id) ?? playerInfo;
                        playerInfo.IsOnline = true;
                        await _playerRepository.AddOrUpdatePlayerAsync(playerInfo);


                        // Broadcast updated online status to all connected clients
                        foreach (var webSocket in _sockets.Values)
                        {
                            if (webSocket.State == WebSocketState.Open)
                            {
                                var _players = await _playerRepository.GetAllPlayersByGameAsync(1);
                                var json = JsonSerializer.Serialize(_players);
                                var bytes = Encoding.UTF8.GetBytes(json);
                                await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _sockets.TryRemove(socketId, out _);
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }
    }

}
