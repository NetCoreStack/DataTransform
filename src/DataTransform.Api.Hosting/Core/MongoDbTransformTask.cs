using Dapper;
using MongoDB.Bson;
using MongoDB.Driver;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class MongoDbTransformTask : ITransformTask
    {
        private readonly DbConnectionFactory<SqlConnection> _sourceSqlDatabase;
        private readonly MongoDbContext _mongoDbDataContext;
        private readonly TransformOptions _options;
        private readonly IConnectionManager _connectionManager;
        private readonly CancellationToken _cancellationToken;

        public List<MongoDbTransformContext> DbTransformContexts { get; }

        public MongoDbTransformTask(TransformOptions options, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _cancellationToken = cancellationToken;

            _sourceSqlDatabase = new DbConnectionFactory<SqlConnection>(options.SourceConnectionString);
            _mongoDbDataContext = new MongoDbContext(options.TargetConnectionString);

            DbTransformContexts = options.CreateMongoDbTransformContexts(_cancellationToken);
        }

        private long GetCount(MongoDbTransformContext context)
        {
            List<dynamic> sqlItems = new List<dynamic>();
            using (var connection = _sourceSqlDatabase.CreateConnection())
            {
                return connection.ExecuteScalar<long>(context.CountSql);
            }
        }

        private async Task<int> TokenizeLoopAsync(MongoDbTransformContext context)
        {
            int take = context.BundleSize;
            object indexId = context.LastIndexId;
            int totalIndices = 0;

            var collectionName = context.CollectionName;
            var identityColumnName = context.IdentityColumnName;

            do
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var predicateSql = $"SELECT TOP {take} {context.FieldPattern} FROM {context.SqlTableNameDialect()} " +
                    $"WHERE {identityColumnName} > {indexId} ORDER BY {identityColumnName} ASC";

                List<dynamic> sqlItems = new List<dynamic>();
                using (var connection = _sourceSqlDatabase.CreateConnection())
                {
                    sqlItems = connection.Query(predicateSql).AsList();
                }

                var itemsCount = sqlItems.Count();
                totalIndices += itemsCount;
                if (itemsCount > 0)
                {
                    var bsonList = sqlItems.Select(p => ((IDictionary<string, object>)p).ToBsonDocument()).ToList();
                    var lastItem = bsonList.LastOrDefault();
                    indexId = lastItem.GetLastId(identityColumnName);

                    var collection = MongoDbQueryHelper.GetOrCreateCollection(_mongoDbDataContext.MongoDatabase, collectionName, context.Collation);

                    await collection.InsertManyAsync(bsonList, new InsertManyOptions
                    {
                        IsOrdered = false
                    });

                    await _connectionManager.WsLogAsync($"Table: {context.TableName} total: {totalIndices} record(s) progressed.");
                }

                if (totalIndices == 0)
                {
                    break;
                }

            } while (totalIndices < context.Count);

            return totalIndices;
        }

        public async Task InvokeAsync()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int totalRecords = 0;

            foreach (var context in DbTransformContexts)
            {   
                object lastIndexId = MongoDbQueryHelper.GetLatestId(_mongoDbDataContext.MongoDatabase, context);
                long count = GetCount(context);
                try
                {
                    context.Count = count;
                    context.LastIndexId = lastIndexId;
                    totalRecords += await TokenizeLoopAsync(context);
                }
                catch (Exception ex)
                {
                    await _connectionManager.WsErrorLog(ex);
                }
                finally
                {
                }
            }

            sw.Stop();
            await _connectionManager.WsLogAsync(string.Format("Transformed total records: {0} time elapsed: {1}", totalRecords, sw.Elapsed));
        }
    }
}
