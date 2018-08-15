using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace DataTransform.Api.Hosting
{
    public class TransformTaskFactory
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConnectionManager _connectionManager;

        public TransformTaskFactory(IHostingEnvironment hostingEnvironment, IConnectionManager connectionManager)
        {
            _hostingEnvironment = hostingEnvironment;
            _connectionManager = connectionManager;
        }

        private ITransformTask CreateTask(TransformOptions options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.SourceConnectionString))
            {
                throw new ArgumentNullException(nameof(options.SourceConnectionString));
            }

            if (string.IsNullOrEmpty(options.TargetConnectionString))
            {
                throw new ArgumentNullException(nameof(options.TargetConnectionString));
            }

            if (options.TargetConnectionString.StartsWith("mongodb"))
            {
                return new MongoDbTransformTask(options, _connectionManager, cancellationToken);
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(options.TargetConnectionString);
                return new SqlTransformTask(options, _connectionManager, cancellationToken);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<ITransformTask> Create(string[] files, CancellationToken cancellationToken)
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

                taskList.Add(CreateTask(options, cancellationToken));

            }

            return taskList;
        }
    }
}
