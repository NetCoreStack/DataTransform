using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;

namespace DataTransform.Api.Hosting
{
    public static class TransformOptionsExtensions
    {
        public static List<MongoDbTransformContext> CreateMongoDbTransformContexts(this TransformOptions options, CancellationToken cancellationToken)
        {
            List<MongoDbTransformContext> transformContexts = new List<MongoDbTransformContext>();

            Collation collation = null;
            if (!string.IsNullOrEmpty(options.Locale))
            {
                collation = new Collation(options.Locale, strength: CollationStrength.Secondary);
            }
            
            foreach (var map in options.Maps)
            {
                var context = new MongoDbTransformContext
                {
                    SelectSql = map.CreateSqlScript(out string fieldsPattern),
                    CountSql = map.CreateCountScript(),
                    CollectionName = map.CollectionName,
                    FieldPattern = fieldsPattern,
                    IdentityColumnName = map.IdentityColumnName,
                    TableName = map.TableName,
                    CancellationToken = cancellationToken,
                    Collation = collation
                };

                transformContexts.Add(context);
            }

            return transformContexts;
        }

        public static List<SqlTransformContext> CreateSqlTransformContexts(this TransformOptions options, CancellationToken cancellationToken)
        {
            List<SqlTransformContext> transformContexts = new List<SqlTransformContext>();

            foreach (var map in options.Maps)
            {
                var context = new SqlTransformContext
                {
                    SelectSql = map.CreateSqlScript(out string fieldsPattern),
                    CountSql = map.CreateCountScript(),
                    FieldPattern = fieldsPattern,
                    IdentityColumnName = map.IdentityColumnName,
                    TableName = map.TableName,
                    CancellationToken = cancellationToken
                };

                transformContexts.Add(context);
            }

            return transformContexts;
        }
    }
}
