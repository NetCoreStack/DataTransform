using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DataTransform.Api.Hosting
{
    public class SqlDatabase
    {
        protected string ConnectionString { get; }

        public SqlDatabase(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public SqlConnection CreateConnection()
        {
            SqlConnection connection = new SqlConnection(ConnectionString);
            connection.Open();
            return connection;
        }

        public int ExecuteNonQuery(string sql, object parameters, int timeout = 0)
        {
            using (var connection = CreateConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    if (timeout > 0)
                    {
                        cmd.CommandTimeout = timeout;
                    }

                    return new CommandDatabase(cmd).ExecuteNonQuery(sql, parameters);
                }
            }
        }

        public IEnumerable<T> ExecuteDataReader<T>(string sql, object parameters, Func<DbDataReader, T> action)
        {
            using (var connection = CreateConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    var db = new CommandDatabase(cmd);
                    foreach (var r in db.ExecuteDataReader(sql, parameters, action))
                        yield return r;
                }
            }
        }

        public void ExecuteDataReader(string sql, object parameters, Action<DbDataReader> action)
        {
            using (var connection = CreateConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    var db = new CommandDatabase(cmd);
                    db.ExecuteDataReader(sql, parameters, action);
                }
            }
        }

        public T ExecuteScalar<T>(string sql, object parameters)
        {
            using (var connection = CreateConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    var db = new CommandDatabase(cmd);
                    return db.ExecuteScalar<T>(sql, parameters);
                }
            }
        }

        public bool HasRow(string sql, object parameters)
        {
            using (var connection = CreateConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    var db = new CommandDatabase(cmd);
                    return db.HasRow(sql, parameters);
                }
            }
        }
    }
}