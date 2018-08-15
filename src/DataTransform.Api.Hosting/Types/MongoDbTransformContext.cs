using MongoDB.Driver;

namespace DataTransform.Api.Hosting
{
    public class MongoDbTransformContext : DbTransformContextBase, IMongoDbTransformContext
    {
        public string CollectionName { get; set; }

        public Collation Collation { get; set; }
    }
}
