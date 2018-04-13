using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataTransform.Api.Hosting
{
    public static class TransformOptionsExtensions
    {
        public static List<DbTransformContext> CreateTransformContexts(this TransformOptions options, CancellationTokenSource cancellationToken)
        {
            List<DbTransformContext> transformContexts = new List<DbTransformContext>();

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
                    CancellationTokenSource = cancellationToken
                };

                transformContexts.Add(context);
            }

            return transformContexts;
        }
    }
}
