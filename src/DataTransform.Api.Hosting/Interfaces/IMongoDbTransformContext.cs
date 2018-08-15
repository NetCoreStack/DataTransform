using MongoDB.Driver;

namespace DataTransform.Api.Hosting
{
    public interface IMongoDbTransformContext : ITransformContext
    {
        string CollectionName { get; set; }

        Collation Collation { get; set; }
    }
}
