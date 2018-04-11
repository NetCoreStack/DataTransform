using Dapper;
using DataTransform.SharedLibrary;
using MongoDB.Bson;
using MongoDB.Driver;
using NetCoreStack.Data.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class DbTransformTask : ITransformTask, IDisposable
    {
        private readonly SqlDatabase _sourceSqlDatabase;
        private readonly IMongoDbDataContext _mongoDbDataContext;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public bool IsShutdownRequested => _cts.IsCancellationRequested;

        public DbTransformTask(SqlDatabase sourceSqlDatabase, IMongoDbDataContext mongoDbDataContext)
        {
            _sourceSqlDatabase = sourceSqlDatabase;
            _mongoDbDataContext = mongoDbDataContext;
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
                if (IsShutdownRequested)
                {
                    break;
                }

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
                totalRecords = await TokenizeLoopAsync(new DbTransformTokenizeContext {
                    Count = count,
                    CollectionName = context.CollectionName,
                    IdentityColumnName = context.IdentityColumnName,
                    BundleSize = context.BundleSize,
                    FieldPattern = context.FieldPattern,
                    TableName = context.TableName,
                    LastIndexId = lastIndexId
                }); 
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                
            }

            sw.Stop();
            Debug.WriteLine("=====MongoDb total records: {0} time elapsed: {1}", totalRecords, sw.Elapsed);
        }

        public async Task InvokeAsync(DbTransformContext context)
        {
            await InvokeInternal(context);
        }

        public void SendStop()
        {
            _cts.Cancel();
        }

        public void Dispose()
        {
            SendStop();
        }
    }
}
