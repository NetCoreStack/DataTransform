using MongoDB.Driver;
using NetCoreStack.Contracts;
using System;
using System.Diagnostics;
using System.Reflection;

namespace DataTransform.Api.Hosting
{
    public class MongoDbContext : IDisposable
    {
        private bool _disposed;
        public string ConnectionString { get; }
        public IMongoDatabase MongoDatabase { get; }

        public MongoDbContext(string connectionString)
        {
            ConnectionString = connectionString;
            var mongoUrl = new MongoUrl(connectionString);            
            var client = new MongoClient(mongoUrl);
            MongoDatabase = client.GetDatabase(mongoUrl.DatabaseName);
        }

        private string GetCollectioNameFromInterface<T>()
        {
            string collectionname;
            var type = typeof(T);
            var attr = type.GetTypeInfo().GetCustomAttribute<CollectionNameAttribute>();
            if (attr != null)
            {
                collectionname = attr.Name;
            }
            else
            {
                collectionname = type.Name;
            }

            return collectionname;
        }

        private string GetCollectionNameFromType(Type type)
        {
            string name;
            var typeInfo = type.GetTypeInfo();
            var attr = typeInfo.GetCustomAttribute<CollectionNameAttribute>();
            if (attr != null)
            {
                name = attr.Name;
            }
            else
            {
                name = typeInfo.Name;
            }

            return name;
        }

        private string GetCollectionName<TEntity>() where TEntity : class
        {
            string name;
            if (typeof(TEntity).GetTypeInfo().BaseType.Equals(typeof(object)))
            {
                name = GetCollectioNameFromInterface<TEntity>();
            }
            else
            {
                name = GetCollectionNameFromType(typeof(TEntity));
            }

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Collection name could not be found!");

            return name;
        }

        public IMongoCollection<TEntity> Collection<TEntity>() where TEntity : class
        {
            return MongoDatabase.GetCollection<TEntity>(GetCollectionName<TEntity>());
        }

        /// <summary>
        ///     Disposes the DbContext.
        /// </summary>
        /// <param name="disposing">
        ///     True to release both managed and unmanaged resources; false to release only unmanaged
        ///     resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // free other managed objects that implement
                    // IDisposable only
                }
            }
            _disposed = true;
        }

        [DebuggerStepThrough]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
