using Microsoft.Extensions.Options;
using NetCoreStack.Data.Interfaces;

namespace DataTransform.Api.Hosting
{
    public class DefaultDataContextConfigurationAccessor : IDataContextConfigurationAccessor
    {
        public string SqlConnectionString { get; }
        public int? CommandTimeout { get; }
        public string MongoDbConnectionString { get; }

        public DefaultDataContextConfigurationAccessor(IOptions<DatabaseOptions> options)
        {
            SqlConnectionString = options.Value.SqlConnectionString;
            MongoDbConnectionString = options.Value.MongoDbConnectionString;
            CommandTimeout = null;
        }
    }
}
