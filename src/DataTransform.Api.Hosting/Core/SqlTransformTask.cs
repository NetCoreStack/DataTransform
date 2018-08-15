using Dapper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using MongoDB.Driver;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class SqlTransformTask : ITransformTask
    {
        private readonly SqlDatabase _sourceSqlDatabase;
        private readonly SqlDatabase _targetSqlDatabase;
        private readonly TransformOptions _options;
        private readonly IConnectionManager _connectionManager;
        private readonly CancellationToken _cancellationToken;
        private readonly SqlConnectionStringBuilder _targetSqlConnectionBuilder;
        public List<SqlTransformContext> DbTransformContexts { get; }

        public SqlTransformTask(TransformOptions options, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _cancellationToken = cancellationToken;

            _sourceSqlDatabase = new SqlDatabase(options.SourceConnectionString);
            _targetSqlDatabase = new SqlDatabase(options.TargetConnectionString);

            _targetSqlConnectionBuilder = new SqlConnectionStringBuilder(options.TargetConnectionString);

            DbTransformContexts = options.CreateSqlTransformContexts(_cancellationToken);
        }

        private long GetCount(SqlTransformContext context)
        {
            List<dynamic> sqlItems = new List<dynamic>();
            using (var connection = _sourceSqlDatabase.CreateConnection())
            {
                return connection.ExecuteScalar<long>(context.CountSql);
            }
        }

        private async Task<int> TokenizeLoopAsync(SqlTransformContext context)
        {
            int take = context.BundleSize;
            object indexId = context.LastIndexId;
            int totalIndices = 0;
            var identityColumnName = context.IdentityColumnName;
            List<string> fields = context.FieldPattern == "*" ? new List<string>(new[] { "*" }) : context.FieldPattern.Split(',').Select(f => f.Trim()).ToList();
            bool allFields = fields.Contains("*");

            DataTable dataTable = new DataTable();
            using (var connection = _sourceSqlDatabase.CreateConnection())
            {
                string targetDatabaseName = _targetSqlConnectionBuilder.InitialCatalog;
                Server server = new Server(new ServerConnection(connection));
                var database = server.Databases[targetDatabaseName];
                var tableName = context.TableName;
                if (tableName.Contains("."))
                {
                    tableName = tableName.Split('.').Last();
                }

                var table = database.Tables[tableName];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var column = table.Columns[i];
                    var name = column.Name;
                    if (allFields)
                    {
                        var dataColumn = new DataColumn(column.Name, column.DataType.SqlDataType.GetClrType());
                        dataTable.Columns.Add(dataColumn);
                    }
                    else
                    {
                        if(fields.Contains(name))
                        {
                            var dataColumn = new DataColumn(column.Name, column.DataType.SqlDataType.GetClrType());
                            dataTable.Columns.Add(dataColumn);
                        }
                    }
                }
            }

            do
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var predicateSql = $"SELECT TOP {take} {context.FieldPattern} FROM {context.TableName} " +
                    $"WHERE {identityColumnName} > {indexId} ORDER BY {identityColumnName} ASC";

                using (var connection = _sourceSqlDatabase.CreateConnection())
                {
                    SqlCommand cmd = new SqlCommand(predicateSql, connection);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    da.Fill(dataTable);
                }

                var itemsCount = dataTable.Rows.Count;
                totalIndices += itemsCount;
                if (itemsCount > 0)
                {
                    DataRow lastRow = dataTable.Rows[itemsCount - 1];
                    indexId = lastRow[identityColumnName];
                    using (var connection = _targetSqlDatabase.CreateConnection())
                    {
                        var tableName = context.TableName;
                        SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(connection.ConnectionString, SqlBulkCopyOptions.KeepIdentity);
                        sqlBulkCopy.DestinationTableName = tableName;
                        sqlBulkCopy.SqlRowsCopied += SqlBulkCopySqlRowsCopied;
                        await sqlBulkCopy.WriteToServerAsync(dataTable);
                        sqlBulkCopy.Close();
                        dataTable.Clear();
                    }

                    await _connectionManager.WsLogAsync($"Table: {context.TableName} total: {totalIndices} record(s) progressed.");
                }

                if (totalIndices == 0)
                {   
                    break;
                }

            } while (totalIndices < context.Count);

            return totalIndices;
        }

        private void SqlBulkCopySqlRowsCopied(object sender, SqlRowsCopiedEventArgs e)
        {
            return;
        }

        private void EnsureDatabase()
        {
            SqlQueryHelper.EnsureTargetStates(_options);
        }

        public async Task InvokeAsync()
        {
            EnsureDatabase();

            Stopwatch sw = new Stopwatch();
            sw.Start();
            int totalRecords = 0;

            foreach (var context in DbTransformContexts)
            {   
                object lastIndexId = SqlQueryHelper.GetLatestId(_targetSqlDatabase, context);
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
