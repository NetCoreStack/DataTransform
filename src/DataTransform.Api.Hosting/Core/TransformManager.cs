using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NetCoreStack.Data;
using NetCoreStack.Data.Context;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static DataTransform.SharedLibrary.HostingConstants;

namespace DataTransform.Api.Hosting
{
    public class TransformManager
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConnectionManager _connectionManager;
        private readonly ICollectionNameSelector _collectionNameSelector;

        public CancellationTokenSource CancellationToken { get; }

        public TransformManager(IHostingEnvironment hostingEnvironment,
            IConnectionManager connectionManager,
            ICollectionNameSelector collectionNameSelector)
        {
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _collectionNameSelector = collectionNameSelector;
            CancellationToken = new CancellationTokenSource();
        }

        public async Task TransformAsync(string filename)
        {
            var configFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", filename);
            if (!System.IO.File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"{configFilePath} not found.");
            }

            SharedSemaphoreSlim.Wait();
            try
            {
                var configuration = new ConfigurationBuilder().AddJsonFile(configFilePath).Build();
                var options = new TransformOptions();
                configuration.Bind(nameof(TransformOptions), options);

                List<DbTransformContext> transformContexts = options.CreateTransformContexts(CancellationToken);

                var dataContextConfigurationAccessor = new DefaultDataContextConfigurationAccessor(options);
                var sqlDatabase = new SqlDatabase(dataContextConfigurationAccessor);
                var mongoDbContext = new MongoDbContext(dataContextConfigurationAccessor, _collectionNameSelector, null);

                ITransformTask transformTask = new DbTransformTask(sqlDatabase, mongoDbContext, _connectionManager);

                foreach (var context in transformContexts)
                {
                    await transformTask.InvokeAsync(context);
                }
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