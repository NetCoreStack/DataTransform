using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;

namespace DataTransform.Api.Hosting
{
    public static class MongoDbQueryHelper
    {
        public static object GetLatestId(this IMongoDatabase mongoDatabase, string collectionName, string identityColumnName)
        {
            var collection = mongoDatabase.GetOrCreateCollection(collectionName);
            var sortBuilder = Builders<BsonDocument>.Sort;
            var sort = sortBuilder.Descending(identityColumnName);
            var item = collection.Find(Builders<BsonDocument>.Filter.Empty).Sort(sort).Limit(1).FirstOrDefault();
            if (item == null)
            {
                return 0;
            }

            var identityValue = item[identityColumnName];

            if (identityValue.IsString)
            {
                return identityValue.AsString;
            }
            else if(identityValue.IsInt32)
            {
                return identityValue.AsInt32;
            }
            else
            {
                return identityValue.AsInt64;
            }
        }

        public static IMongoCollection<BsonDocument> GetOrCreateCollection(this IMongoDatabase mongoDatabase, string collectionName)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = mongoDatabase.ListCollections(new ListCollectionsOptions { Filter = filter });
            if (!collections.Any())
            {
                mongoDatabase.CreateCollection(collectionName);
            }

            return mongoDatabase.GetCollection<BsonDocument>(collectionName);
        }
    }
}
