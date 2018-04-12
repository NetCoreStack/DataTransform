using NetCoreStack.WebSockets;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public static class ConnectionManagerExtensions
    {
        public static async Task WsLogAsync(this IConnectionManager connectionManager, string message)
        {
            await connectionManager.BroadcastAsync(new WebSocketMessageContext
            {
                Command = WebSocketCommands.DataSend,
                Value = new { resultState = "success", message },
                MessageType = WebSocketMessageType.Text
            });
        }

        public static async Task WsErrorLog(this IConnectionManager connectionManager, Exception ex)
        {
            await connectionManager.BroadcastAsync(new WebSocketMessageContext
            {
                Command = WebSocketCommands.DataSend,
                Value = new { resultState = "error", message = ex.Message },
                MessageType = WebSocketMessageType.Text
            });
        }
    }
}
