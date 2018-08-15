using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;

namespace DataTransform.Api.Hosting
{
    public static class MongoDbQueryHelper
    {
        // case insensitive index 
        // https://docs.mongodb.com/manual/core/index-case-insensitive/
        public static Collation Collation => new Collation("tr", strength: CollationStrength.Secondary);

        public static object GetLastId(this BsonDocument document, string identityColumnName)
        {
            var identityValue = document[identityColumnName];

            if (identityValue.IsString)
            {
                return identityValue.AsString;
            }
            else if (identityValue.IsInt32)
            {
                return identityValue.AsInt32;
            }
            else
            {
                return identityValue.AsInt64;
            }
        }

        public static object GetLatestId(this IMongoDatabase mongoDatabase, MongoDbTransformContext context)
        {
            var collection = mongoDatabase.GetOrCreateCollection(context.CollectionName, context.Collation);
            var sortBuilder = Builders<BsonDocument>.Sort;
            var sort = sortBuilder.Descending(context.IdentityColumnName);
            var item = collection.Find(Builders<BsonDocument>.Filter.Empty).Sort(sort).Limit(1).FirstOrDefault();
            if (item == null)
            {
                return 0;
            }

            return item.GetLastId(context.IdentityColumnName);
        }

        public static IMongoCollection<BsonDocument> GetOrCreateCollection(this IMongoDatabase mongoDatabase, string collectionName, Collation collation)
        {
            var filter = new BsonDocument("name", collectionName);
            var collections = mongoDatabase.ListCollections(new ListCollectionsOptions { Filter = filter });
            if (!collections.Any())
            {
                if (collation != null)
                {
                    mongoDatabase.CreateCollection(collectionName, new CreateCollectionOptions
                    {
                        Collation = collation
                    });
                }
                else
                {
                    mongoDatabase.CreateCollection(collectionName);
                }                
            }

            return mongoDatabase.GetCollection<BsonDocument>(collectionName);
        }
    }
}
