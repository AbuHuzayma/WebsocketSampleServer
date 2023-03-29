using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleClient
{
    public class ClientService
    {
        private static readonly Uri _serverUri = new Uri("wss://localhost:44349/");
        private static readonly ClientWebSocket _clientWebSocket = new ClientWebSocket();
        public ClientService()
        { }

        public async Task CallMain()
        {
            try
            {
                await _clientWebSocket.ConnectAsync(_serverUri, CancellationToken.None);
                Console.WriteLine("Connected to server.");

                // Send player info to server
                var playerInfo = new PlayerInfo
                { 
                    Id=1,
                    Name = "Alice",
                    IsOnline = true
                };
                await SendMessageAsync(playerInfo);

                // Receive response from server
                var response = await ReceiveMessageAsync();
                Console.WriteLine($"Received message from server: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
        public async Task SendMessageAsync(PlayerInfo playerInfo)
        {
            var message = JsonSerializer.Serialize(playerInfo);
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);
            await _clientWebSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Sent message to server: {message}");
        }
        public async Task<string> ReceiveMessageAsync()
        {
            var buffer = new byte[1024 * 4];
            var segment = new ArraySegment<byte>(buffer);
            var result = await _clientWebSocket.ReceiveAsync(segment, CancellationToken.None);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return message;
        }
    }
}
