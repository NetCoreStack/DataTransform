using Dapper;
using DataTransform.SharedLibrary;
using MongoDB.Bson;
using MongoDB.Driver;
using NetCoreStack.Data.Interfaces;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class DbTransformTask : ITransformTask
    {
        private readonly SqlDatabase _sourceSqlDatabase;
        private readonly IMongoDbDataContext _mongoDbDataContext;
        private readonly IConnectionManager _connectionManager;

        public DbTransformTask(SqlDatabase sourceSqlDatabase, 
            IMongoDbDataContext mongoDbDataContext,
            IConnectionManager connectionManager)
        {
            _sourceSqlDatabase = sourceSqlDatabase;
            _mongoDbDataContext = mongoDbDataContext;
            _connectionManager = connectionManager;
        }

        private long GetCount(DbTransformContext context)
        {
            List<dynamic> sqlItems = new List<dynamic>();
            using (var connection = _sourceSqlDatabase.CreateConnection())
            {
                return connection.ExecuteScalar<long>(context.CountSql);
            }
        }

        private async Task TokenizeAsync(string collectionName, List<dynamic> items)
        {
            if (items.Any())
            {
                var collection = MongoDbQueryHelper.GetOrCreateCollection(_mongoDbDataContext.MongoDatabase, collectionName);

                var bsonList = items.Select(p => ((IDictionary<string, object>)p).ToBsonDocument()).ToList();

                await collection.InsertManyAsync(bsonList, new InsertManyOptions
                {
                    IsOrdered = false
                });
            }
        }

        private async Task<int> TokenizeLoopAsync(DbTransformTokenizeContext context)
        {
            int take = context.BundleSize;
            object indexId = context.LastIndexId;
            int totalIndices = 0;
            object rangeIndex = 0;

            do
            {
                if (context.CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await _connectionManager.WsLogAsync($"TableName: {context.TableName} counts: {context.Count} record(s) processing...");

                var predicateSql = $"SELECT TOP {take} {context.FieldPattern} FROM {context.TableName} WHERE {context.IdentityColumnName} > {indexId}";
                List<dynamic> sqlItems = new List<dynamic>();
                using (var connection = _sourceSqlDatabase.CreateConnection())
                {
                    sqlItems = connection.Query(predicateSql).AsList();
                }

                var itemsCount = sqlItems.Count();
                totalIndices += itemsCount;
                if (itemsCount > 0)
                {
                    await TokenizeAsync(context.CollectionName, sqlItems);
                    await _connectionManager.WsLogAsync($"TableName: {context.TableName} Total: {context.Count} record(s) progress.");
                }
                indexId = rangeIndex;

            } while (totalIndices < context.Count);

            return totalIndices;
        }

        private async Task InvokeInternal(DbTransformContext context)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int totalRecords = 0;
            object lastIndexId = MongoDbQueryHelper.GetLatestId(_mongoDbDataContext.MongoDatabase, context.CollectionName, context.IdentityColumnName);
            long count = GetCount(context);
            try
            {
                var tokenizedContext = new DbTransformTokenizeContext
                {
                    Count = count,
                    CollectionName = context.CollectionName,
                    IdentityColumnName = context.IdentityColumnName,
                    BundleSize = context.BundleSize,
                    FieldPattern = context.FieldPattern,
                    TableName = context.TableName,
                    LastIndexId = lastIndexId,
                    CancellationTokenSource = context.CancellationTokenSource
                };

                totalRecords = await TokenizeLoopAsync(tokenizedContext); 
            }
            catch (Exception ex)
            {
                await _connectionManager.WsErrorLog(ex);
            }
            finally
            {
                
            }
            sw.Stop();

            await _connectionManager.WsLogAsync(string.Format("MongoDb total records: {0} time elapsed: {1}", totalRecords, sw.Elapsed));
        }

        public async Task InvokeAsync(DbTransformContext context)
        {
            await InvokeInternal(context);
        }
    }
}
