using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;

namespace DataTransform.Api.Hosting
{
    public static class TransformOptionsExtensions
    {
        public static List<DbTransformContext> CreateTransformContexts(this TransformOptions options, CancellationTokenSource cancellationToken)
        {
            List<DbTransformContext> transformContexts = new List<DbTransformContext>();

            Collation collation = null;
            if (!string.IsNullOrEmpty(options.Locale))
            {
                collation = new Collation(options.Locale, strength: CollationStrength.Secondary);
            }
            
            foreach (var map in options.Maps)
            {
                var context = new DbTransformContext
                {
                    SelectSql = map.CreateSqlScript(out string fieldsPattern),
                    CountSql = map.CreateCountScript(),
                    CollectionName = map.CollectionName,
                    FieldPattern = fieldsPattern,
                    IdentityColumnName = map.IdentityColumnName,
                    TableName = map.TableName,
                    CancellationTokenSource = cancellationToken,
                    Collation = collation
                };

                transformContexts.Add(context);
            }

            return transformContexts;
        }
    }
}
