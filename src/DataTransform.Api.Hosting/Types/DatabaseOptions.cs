using Microsoft.Extensions.Options;

namespace DataTransform.Api.Hosting
{
    public class DatabaseOptions : IOptions<DatabaseOptions>
    {
        public string SqlConnectionString { get; set; }
        public string MongoDbConnectionString { get; set; }

        public DatabaseOptions Value => this;

        public DatabaseOptions()
        {
        }
    }
}
