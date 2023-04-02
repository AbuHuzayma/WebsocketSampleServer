using EdiyaGameWebsocket.Data.Repository;
using EdiyaGameWebsocket.Entities;
using EdiyaGameWebsocket.Middleware.Dto;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EdiyaGameWebsocket.Middleware
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private readonly IPlayerRepository _playerRepository;
        public WebSocketMiddleware(RequestDelegate next, IPlayerRepository playerRepository)
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

            // Get player info from query parameter
            var gameId = context.Request.Query["gameId"].ToString();
            var authHeaders = context.Request.Headers["authHeaders"].ToString();
            if (string.IsNullOrWhiteSpace(gameId))
            {
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Player ID is required.", CancellationToken.None);
                return;
            }

            // Add socket to dictionary
            _sockets.TryAdd(gameId+"_"+ socketId, socket);

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
                        var playerInfo = JsonSerializer.Deserialize<PlayerDto>(message);

                        // Update player's online status in database
                        var dbPlayer = await _playerRepository.GetPlayerAsync(playerInfo.Id);
                        dbPlayer.IsOnline = playerInfo.IsOnline;
                        await _playerRepository.AddOrUpdatePlayerAsync(dbPlayer);

                        // Broadcast updated online status to all connected clients
                        var _players = await _playerRepository.GetAllPlayersByGameAsync(int.Parse(gameId));
                        var json = JsonSerializer.Serialize(_players);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        foreach (var kvp in _sockets)
                        {
                            if (kvp.Key.StartsWith(gameId+"_"))
                            {
                                try
                                {
                                    // Send message to specific client based on game Id
                                    await SendMessageAsync(kvp.Key, json);
                                }
                                catch (Exception ex)
                                {
                                    _sockets.TryRemove(socketId, out _);
                                    Console.WriteLine($"WebSocket error: {ex.Message}");
                                }
                                
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

        public async Task SendMessageAsync(string playerId, string message)
        {
            if (_sockets.TryGetValue(playerId, out WebSocket socket))
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

}
