using System;
using System.Data.Common;

namespace DataTransform.Api.Hosting
{
    public interface IConnectionFactory
    {
        DbConnection CreateConnection();
        Type DbConnectionType { get; }
    }
}
