using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace DataTransform.Api.Hosting
{
    internal class CommandDatabase
    {
        public readonly DbCommand Command;

        public CommandDatabase(DbCommand cmd)
        {
            Command = cmd;
        }

        private void PrepareCommand(string sql, object parameters)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = sql;
            Command.SetParameters(parameters);
        }

        public int ExecuteNonQuery(string sql, object parameters, int timeout = 0)
        {
            PrepareCommand(sql, parameters);
            return Command.ExecuteNonQuery();
        }

        public IEnumerable<T> ExecuteDataReader<T>(string sql, object parameters, Func<DbDataReader, T> action)
        {
            PrepareCommand(sql, parameters);

            using (var dr = Command.ExecuteReader())
            {
                while (dr.Read())
                    yield return action.Invoke(dr);
            }
        }

        public void ExecuteDataReader(string sql, object parameters, Action<DbDataReader> action)
        {
            PrepareCommand(sql, parameters);

            using (var dr = Command.ExecuteReader())
            {
                while (dr.Read())
                    action.Invoke(dr);
            }
        }

        public T ExecuteScalar<T>(string sql, object parameters)
        {
            PrepareCommand(sql, parameters);

            return (T)Command.ExecuteScalar();
        }

        public bool HasRow(string sql, object parameters)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = sql;
            Command.SetParameters(parameters);

            using (var dr = Command.ExecuteReader())
            {
                return dr.HasRows;
            }
        }
    }
}