using System;
using System.Data.Common;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;

namespace DataTransform.Api.Hosting
{
    public static class DbCommandExtensions
    {
        public static void SetParameters(this DbCommand cmd, object parameters)
        {
            cmd.Parameters.Clear();

            if (parameters == null)
                return;

            if (parameters is IList)
            {
                var listed = (IList)parameters;
                for (var i = 0; i < listed.Count; i++)
                {
                    AddParameter(cmd, string.Format("@{0}", i), listed[i]);
                }
            }
            else if (parameters is IDictionary)
            {
                var dictionary = (IDictionary<string, object>)parameters;
                foreach (KeyValuePair<string, object> item in dictionary)
                {
                    AddParameter(cmd, item.Key, item.Value);
                }
            }
            else
            {
                var t = parameters.GetType();
                var parameterInfos = t.GetTypeInfo().GetProperties();
                foreach (var pi in parameterInfos)
                {
                    AddParameter(cmd, pi.Name, pi.GetValue(parameters, null));
                }
            }
        }

        private static void AddParameter(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}