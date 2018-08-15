using Dapper;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Specialized;
using System.Data.SqlClient;

namespace DataTransform.Api.Hosting
{
    public static class SqlQueryHelper
    {
        public static void EnsureTargetStates(TransformOptions options)
        {
            var sourceBuilder = new SqlConnectionStringBuilder(options.SourceConnectionString);
            var targetBuilder = new SqlConnectionStringBuilder(options.TargetConnectionString);

            Server sourceServer = null;
            if (sourceBuilder.IntegratedSecurity)
            {
                sourceServer = new Server(sourceBuilder.DataSource);
            }
            else
            {
                sourceServer = new Server(new ServerConnection(sourceBuilder.DataSource, sourceBuilder.UserID, sourceBuilder.Password));
            }

            Server targetServer = null;
            if (targetBuilder.IntegratedSecurity)
            {
                targetServer = new Server(targetBuilder.DataSource);
            }
            else
            {
                targetServer = new Server(new ServerConnection(targetBuilder.DataSource, targetBuilder.UserID, targetBuilder.Password));
            }

            string sourceDatabaseName = sourceBuilder.InitialCatalog;
            string targetDatabaseName = targetBuilder.InitialCatalog;

            if (!sourceServer.Databases.Contains(sourceDatabaseName))
            {
                throw new InvalidOperationException($"Source database does not exist: {sourceDatabaseName}");
            }

            Database sourceDatabase = sourceServer.Databases[sourceDatabaseName];
            Transfer transfer = new Transfer(sourceDatabase);

            Database targetDatabase = null;
            if(!targetServer.Databases.Contains(targetDatabaseName))
            {
                targetDatabase = new Database(targetServer, targetDatabaseName);
                targetDatabase.Create();
            }
            else
            {
                targetDatabase = targetServer.Databases[targetDatabaseName];
            }

            ScriptingOptions scriptOptions = new ScriptingOptions();
            scriptOptions.IncludeIfNotExists = true;
            scriptOptions.WithDependencies = true; 
            scriptOptions.Indexes = true;
            scriptOptions.DriForeignKeys = true;
            scriptOptions.Triggers = true;

            transfer.Options = scriptOptions;
            transfer.DestinationServer = targetServer.Name;
            transfer.DestinationDatabase = targetDatabaseName;
            transfer.CopyAllUsers = true;
            StringCollection scripts = transfer.ScriptTransfer();
            scripts.Insert(0, $"USE [{targetDatabaseName}]");

            targetServer.ConnectionContext.ExecuteNonQuery(scripts);
        }

        public static object GetLatestId(this SqlDatabase targetDatabase, SqlTransformContext context)
        {
            var predicateSql = $"SELECT {context.IdentityColumnName} FROM {context.TableName} ORDER BY {context.IdentityColumnName} DESC";
            long lastId = 0;
            using (var connection = targetDatabase.CreateConnection())
            {
                lastId = connection.ExecuteScalar<long>(predicateSql);
            }

            return lastId;
        }

        public static Type GetClrType(this SqlDataType sqlType)
        {
            switch (sqlType)
            {
                case SqlDataType.BigInt:
                    return typeof(System.Int64);

                case SqlDataType.Binary:
                case SqlDataType.Image:
                case SqlDataType.Timestamp:
                case SqlDataType.VarBinary:
                    return typeof(byte[]);

                case SqlDataType.Bit:
                    return typeof(System.Boolean);

                case SqlDataType.Char:
                case SqlDataType.NChar:
                case SqlDataType.NText:
                case SqlDataType.NVarChar:
                case SqlDataType.Text:
                case SqlDataType.VarChar:
                case SqlDataType.NVarCharMax:
                case SqlDataType.VarCharMax:
                case SqlDataType.Xml:
                    return typeof(System.String);

                case SqlDataType.DateTime:
                case SqlDataType.SmallDateTime:
                case SqlDataType.Date:
                case SqlDataType.Time:
                case SqlDataType.DateTime2:
                    return typeof(DateTime);

                case SqlDataType.Decimal:
                case SqlDataType.Money:
                case SqlDataType.SmallMoney:
                    return typeof(System.Decimal);

                case SqlDataType.Int:
                    return typeof(System.Int32);

                case SqlDataType.Float:
                case SqlDataType.Numeric:
                case SqlDataType.Real:
                    return typeof(System.Double);

                case SqlDataType.UniqueIdentifier:
                    return typeof(Guid);

                case SqlDataType.TinyInt:
                case SqlDataType.SmallInt:
                    return typeof(System.Int16);

                case SqlDataType.UserDefinedDataType:
                case SqlDataType.Variant:
                case SqlDataType.UserDefinedType:
                    return typeof(object);

                case SqlDataType.DateTimeOffset:
                    return typeof(DateTimeOffset);

                default:
                    throw new ArgumentOutOfRangeException("sqlType");
            }
        }
    }
}
