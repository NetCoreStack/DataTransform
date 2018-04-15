using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NetCoreStack.Data;
using NetCoreStack.WebSockets;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DataTransform.Api.Hosting
{
    public class TransformTaskFactory
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConnectionManager _connectionManager;
        private readonly ICollectionNameSelector _collectionNameSelector;

        public TransformTaskFactory(IHostingEnvironment hostingEnvironment, 
            IConnectionManager connectionManager, 
            ICollectionNameSelector collectionNameSelector)
        {
            _hostingEnvironment = hostingEnvironment;
            _connectionManager = connectionManager;
            _collectionNameSelector = collectionNameSelector;
        }

        public List<ITransformTask> Create(string[] files, CancellationTokenSource cancellationToken)
        {
            List<ITransformTask> taskList = new List<ITransformTask>();
            foreach (var file in files)
            {
                var configFilePath = Path.Combine(_hostingEnvironment.WebRootPath, "configs", file);
                if (!File.Exists(configFilePath))
                {
                    throw new FileNotFoundException($"{configFilePath} not found.");
                }

                var configuration = new ConfigurationBuilder().AddJsonFile(configFilePath).Build();
                var options = new TransformOptions();
                configuration.Bind(nameof(TransformOptions), options);

                taskList.Add(new DbTransformTask(options, _collectionNameSelector, _connectionManager, cancellationToken));
            }

            return taskList;
        }
    }
}
