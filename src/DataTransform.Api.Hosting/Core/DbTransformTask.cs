﻿using Dapper;
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

        private async Task<int> TokenizeLoopAsync(DbTransformContext context)
        {
            int take = context.BundleSize;
            object indexId = context.LastIndexId;
            int totalIndices = 0;

            var collectionName = context.CollectionName;
            var identityColumnName = context.IdentityColumnName;

            do
            {
                if (context.CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                await _connectionManager.WsLogAsync($"SQL Table: {context.TableName} counts: {context.Count} record(s) processing...");

                var predicateSql = $"SELECT TOP {take} {context.FieldPattern} FROM {context.TableName} WHERE {identityColumnName} > {indexId}";
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

                    var collection = MongoDbQueryHelper.GetOrCreateCollection(_mongoDbDataContext.MongoDatabase, collectionName);

                    await collection.InsertManyAsync(bsonList, new InsertManyOptions
                    {
                        IsOrdered = false
                    });

                    await _connectionManager.WsLogAsync($"SQL Table: {context.TableName} total: {totalIndices} record(s) progressed.");
                }

                if (totalIndices == 0)
                {
                    break;
                }

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
                context.Count = count;
                context.LastIndexId = lastIndexId;
                totalRecords = await TokenizeLoopAsync(context); 
            }
            catch (Exception ex)
            {
                await _connectionManager.WsErrorLog(ex);
            }
            finally
            {
                
            }
            sw.Stop();

            await _connectionManager.WsLogAsync(string.Format("Transformed total records: {0} time elapsed: {1}", totalRecords, sw.Elapsed));
        }

        public async Task InvokeAsync(DbTransformContext context)
        {
            await InvokeInternal(context);
        }
    }
}
