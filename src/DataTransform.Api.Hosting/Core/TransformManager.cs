using DataTransform.SharedLibrary;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static DataTransform.SharedLibrary.HostingConstants;

namespace DataTransform.Api.Hosting
{
    public class TransformManager
    {
        private readonly TransformOptions _options;
        private readonly ITransformTask _transformTask;

        public CancellationTokenSource CancellationToken { get; }

        public TransformManager(IOptionsSnapshot<TransformOptions> options, ITransformTask transformTask)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Value == null)
            {
                throw new ArgumentNullException(nameof(options.Value));
            }

            _options = options.Value;
            _transformTask = transformTask;
            CancellationToken = new CancellationTokenSource();
        }

        public async Task TransformAsync()
        {
            List<DbTransformContext> transformContexts = new List<DbTransformContext>();
            foreach (var map in _options.Maps)
            {
                var context = new DbTransformContext
                {
                    SelectSql = map.CreateSqlScript(out string fieldsPattern),
                    CountSql = map.CreateCountScript(),
                    CollectionName = map.CollectionName,
                    FieldPattern = fieldsPattern,
                    IdentityColumnName = map.IdentityColumnName,
                    TableName = map.TableName,
                    CancellationTokenSource = CancellationToken
                };

                transformContexts.Add(context);
            }

            SharedSemaphoreSlim.Wait();
            try
            {
                foreach (var context in transformContexts)
                {
                    await _transformTask.InvokeAsync(context);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                SharedSemaphoreSlim.Release();
            }
        }
    }
}