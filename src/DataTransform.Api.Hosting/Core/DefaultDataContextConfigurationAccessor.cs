using NetCoreStack.Data.Interfaces;

namespace DataTransform.Api.Hosting
{
    public class DefaultDataContextConfigurationAccessor : IDataContextConfigurationAccessor
    {
        public string SqlConnectionString { get; }
        public int? CommandTimeout { get; }
        public string MongoDbConnectionString { get; }

        public DefaultDataContextConfigurationAccessor(TransformOptions options)
        {
            SqlConnectionString = options.SqlConnectionString;
            MongoDbConnectionString = options.MongoDbConnectionString;
            CommandTimeout = null;
        }
    }
}
