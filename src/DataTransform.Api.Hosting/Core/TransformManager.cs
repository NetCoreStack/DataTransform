using NetCoreStack.WebSockets;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static DataTransform.SharedLibrary.HostingConstants;

namespace DataTransform.Api.Hosting
{
    public class TransformManager
    {
        private readonly IConnectionManager _connectionManager;
        private readonly TransformTaskFactory _transformTaskFactory;

        public TransformManager(TransformTaskFactory transformTaskFactory, IConnectionManager connectionManager)
        {
            _transformTaskFactory = transformTaskFactory ?? throw new ArgumentNullException(nameof(transformTaskFactory));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public async Task TransformAsync(string[] files, CancellationToken cancellationToken)
        {   
            SharedSemaphoreSlim.Wait();
            try
            {
                var tasks = _transformTaskFactory.Create(files, cancellationToken);
                await Task.WhenAll(tasks.Select(t => t.InvokeAsync()));
            }
            catch (Exception ex)
            {
                await _connectionManager.WsErrorLog(ex);
            }
            finally
            {
                SharedSemaphoreSlim.Release();
                await _connectionManager.WsTransformCompleted();
            }
        }
    }
}