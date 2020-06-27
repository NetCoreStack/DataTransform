using Dapper;
using MongoDB.Driver;
using NetCoreStack.WebSockets;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public class SqlTransformTask : ITransformTask
    {
        private readonly DbConnectionFactory<SqlConnection> _sourceSqlDatabase;
        private readonly DbConnectionFactory<SqlConnection> _targetSqlDatabase;
        private readonly TransformOptions _options;
        private readonly IConnectionManager _connectionManager;
        private readonly CancellationToken _cancellationToken;
        private readonly SqlConnectionStringBuilder _targetSqlConnectionBuilder;
        public List<SqlTransformContext> DbTransformContexts { get; private set; }

        public SqlTransformTask(TransformOptions options, IConnectionManager connectionManager, CancellationToken cancellationToken)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _cancellationToken = cancellationToken;

            _sourceSqlDatabase = new DbConnectionFactory<SqlConnection>(options.SourceConnectionString);
            _targetSqlDatabase = new DbConnectionFactory<SqlConnection>(options.TargetConnectionString);

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

        private List<string> CreateInsertScript(string tableName, 
            string columnsOrder, 
            IDictionary<string, TableColumnMetadata> columns, 
            IEnumerable<IDictionary<string, object>> values)
        {
            var insertSql = $"INSERT INTO {tableName} ({columnsOrder}) VALUES ";
            var batchSqlScripts = new List<string>();

            foreach (IDictionary<string, object> entry in values)
            {
                var sb = new StringBuilder();
                sb.Append(insertSql);
                sb.Append("(");

                List<string> sqlValues = new List<string>();
                foreach (KeyValuePair<string, object> item in entry)
                {
                    if (!columns.TryGetValue(item.Key, out TableColumnMetadata metadata))
                    {
                        throw new InvalidOperationException("Unmatch field!");
                    }

                    var value = SqlTypeHelper.GetSqlValue(item.Value);
                    sqlValues.Add(value);
                }

                sb.Append(string.Join(",", sqlValues));
                sb.Append(")");
                sb.AppendLine();
                batchSqlScripts.Add(sb.ToString());
            }

            return batchSqlScripts;
        }

        private async Task<int> TokenizeLoopAsync(SqlTransformContext context)
        {
            int take = context.BundleSize;
            object indexId = context.LastIndexId;
            int totalIndices = 0;
            var identityColumnName = context.IdentityColumnName;           

            IDictionary<string, TableColumnMetadata> columns = new Dictionary<string, TableColumnMetadata>();
            using (DbConnection connection = _sourceSqlDatabase.CreateConnection())
            {
                string targetDatabaseName = _targetSqlConnectionBuilder.InitialCatalog;                
                var tableName = context.TableName;
                if (tableName.Contains("."))
                {
                    tableName = tableName.Split('.').Last();
                }

                var sqlTableInfo = @"SELECT 
    c.name as Name,
    t.Name as DataType,
    c.max_length as MaxLength,
    c.is_nullable as Nullable,
    ISNULL(i.is_primary_key, 0) as IsPrimaryKey
FROM    
    sys.columns c
INNER JOIN 
    sys.types t ON c.user_type_id = t.user_type_id
LEFT OUTER JOIN 
    sys.index_columns ic ON ic.object_id = c.object_id AND ic.column_id = c.column_id
LEFT OUTER JOIN 
    sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
WHERE
    c.object_id = OBJECT_ID(@tableName)";

                columns = connection.Query<TableColumnMetadata>(sqlTableInfo, new { tableName }).ToDictionary(x => x.Name, x => x);
            }

            string columnsOrder = context.FieldPattern == "*" ?
                string.Join(",", columns.Keys.OrderBy(x => x).Select(x => $"[{x}]")) : 
                context.FieldPattern;

            do
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var predicateSql = $"SELECT TOP {take} {columnsOrder} FROM {context.SqlTableNameDialect()} " +
                    $"WHERE {identityColumnName} > {indexId} ORDER BY {identityColumnName} ASC";

                IEnumerable<IDictionary<string, object>> sourceValues = Enumerable.Empty<Dictionary<string, object>>();
                using (var connection = _sourceSqlDatabase.CreateConnection())
                {
                    sourceValues = connection.Query(predicateSql).Select(x => (IDictionary<string, object>)x).ToList();
                }

                var itemsCount = sourceValues.Count();
                totalIndices += itemsCount;
                var tableName = context.SqlTableNameDialect();
                DataTable dataTable = new DataTable(tableName);

                foreach (KeyValuePair<string, TableColumnMetadata> entry in columns)
                {
                    TableColumnMetadata metadata = entry.Value;
                    var column = dataTable.Columns.Add();
                    column.ColumnName = metadata.Name;
                    column.AllowDBNull = metadata.Nullable;
                    column.DataType = metadata.ClrType;     
                }

                foreach (IDictionary<string, object> entry in sourceValues)
                {
                    DataRow row = dataTable.NewRow();
                    foreach (KeyValuePair<string, object> item in entry)
                    {
                        if (!columns.TryGetValue(item.Key, out TableColumnMetadata metadata))
                        {
                            throw new InvalidOperationException("Unmatch field!");
                        }

                        var value = item.Value;
                        row[item.Key] = value == null ? (object)DBNull.Value : value;
                    }
                    dataTable.Rows.Add(row);
                }                

                if (itemsCount > 0)
                {
                    using (var connection = _targetSqlDatabase.CreateConnection())
                    {
                        DataRow lastRow = dataTable.Rows[itemsCount - 1];
                        indexId = lastRow[identityColumnName];
                        using (var targetConnection = _targetSqlDatabase.CreateConnection())
                        {
                            SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(targetConnection.ConnectionString, SqlBulkCopyOptions.KeepIdentity);
                            sqlBulkCopy.DestinationTableName = tableName;
                            sqlBulkCopy.SqlRowsCopied += SqlBulkCopySqlRowsCopied;
                            await sqlBulkCopy.WriteToServerAsync(dataTable);
                            sqlBulkCopy.Close();
                            dataTable.Clear();
                        }
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

        public async Task InvokeAsync()
        {
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

        public async Task InvokeAsync(params SqlTransformContext[] contexts)
        {
            DbTransformContexts = new List<SqlTransformContext>(contexts);
            await InvokeAsync();
        }
    }
}
