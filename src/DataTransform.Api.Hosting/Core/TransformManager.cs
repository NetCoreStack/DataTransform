using DataTransform.SharedLibrary;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class TransformManager
    {
        private readonly TransformOptions _options;
        private readonly ITransformTask _transformTask;

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
        }

        public async Task TransformAsync()
        {
            await Task.CompletedTask;

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
                    TableName = map.TableName
                };

                transformContexts.Add(context);
            }

            foreach (var context in transformContexts)
            {
                await _transformTask.InvokeAsync(context);
            }

            //List<dynamic> sqlItems = new List<dynamic>();
            //using (var connection = _sqlDatabase.CreateConnection())
            //{
            //    sqlItems = connection.Query("SELECT * FROM Albums").AsList();
            //}

            //var items = sqlItems.Select(p => ((IDictionary<string, object>)p).ToBsonDocument()).ToList();
            //var collection = _mongoDbDataContext.MongoDatabase.GetCollection<BsonDocument>("Albums");            

            //collection.InsertMany(items);
        }
    }
}