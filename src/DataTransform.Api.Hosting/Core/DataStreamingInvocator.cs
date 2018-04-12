using NetCoreStack.WebSockets;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class DataStreamingInvocator : IServerWebSocketCommandInvocator
    {
        public async Task InvokeAsync(WebSocketMessageContext context)
        {
            await Task.CompletedTask;
        }
    }
}
