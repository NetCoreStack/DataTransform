using System;
using System.Data.Common;

namespace DataTransform.Api.Hosting
{
    public class DbConnectionFactory<TDbConnection> : IConnectionFactory
            where TDbConnection : DbConnection, new()
    {
        private readonly string _connectionString;

        public Type DbConnectionType => typeof(TDbConnection);

        public DbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public DbConnection CreateConnection()
        {
            var connection = new TDbConnection
            {
                ConnectionString = _connectionString
            };

            return connection;
        }
    }
}