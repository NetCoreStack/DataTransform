using Dapper;
using System;
using System.Data.SqlClient;

namespace DataTransform.Api.Hosting
{
    public static class SqlQueryHelper
    {
        public static object GetLatestId(this DbConnectionFactory<SqlConnection> targetDatabase, SqlTransformContext context)
        {
            var predicateSql = $"SELECT {context.IdentityColumnName} FROM {context.SqlTableNameDialect()} ORDER BY {context.IdentityColumnName} DESC";
            long lastId = 0;
            using (var connection = targetDatabase.CreateConnection())
            {
                lastId = connection.ExecuteScalar<long>(predicateSql);
            }

            return lastId;
        }

        public static Type GetClrType(string dataType)
        {
            switch (dataType)
            {
                case "uniqueidentifier":
                    return typeof(Guid);

                case "varbinary":
                case "varbinarymax":
                    return typeof(byte[]);

                case "bit":
                    return typeof(Boolean);

                case "varchar":
                case "nvarchar":
                case "char":
                case "nchar":
                    return typeof(String);

                case "date":
                case "datetime":
                case "datetime2":
                    return typeof(DateTime);

                case "time":
                    return typeof(TimeSpan);

                case "decimal":
                case "money":
                    return typeof(Decimal);

                case "bigint":
                    return typeof(Int64);

                case "int":
                    return typeof(Int32);

                case "tinyint":
                case "smallint":
                    return typeof(Int16);

                case "float":
                case "numeric":
                    return typeof(Double);               

                case "userdefinedtype":
                case "geometry":
                case "geography":
                    return typeof(object);

                case "datetimeoffset":
                    return typeof(DateTimeOffset);

                default:
                    throw new ArgumentOutOfRangeException("sqlType");
            }
        }
    }
}
