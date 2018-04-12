using DataTransform.SharedLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetCoreStack.Data;
using NetCoreStack.Data.Context;
using NetCoreStack.Data.Interfaces;
using NetCoreStack.Data.Types;
using System.IO;

namespace DataTransform.Api.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTransformFeatures(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions();
            
            services.Configure<DatabaseOptions>(configuration.GetSection(nameof(DatabaseOptions)));

            services.Configure<TransformOptions>(configuration.GetSection(nameof(TransformOptions)));

            services.AddScoped<SqlDatabase>();
            services.AddScoped<IMongoDbDataContext, MongoDbContext>();
            services.AddScoped<IDatabasePreCommitFilter, DefaultDatabasePreCommitFilter>();

            services.AddScoped<ITransformTask, DbTransformTask>();
            services.AddScoped<TransformManager>();

            services.TryAddSingleton<ICollectionNameSelector, DefaultCollectionNameSelector>();
            services.TryAddSingleton<IDataContextConfigurationAccessor, DefaultDataContextConfigurationAccessor>();
        }

        public static void AddTransformConfigFile(this IConfigurationBuilder builder, IHostingEnvironment hostingEnvironment)
        {
            HostingConstants.TransformJsonFileFullPath = Path.Combine(hostingEnvironment.WebRootPath, "configs", "transform.json");
            builder.AddJsonFile(HostingConstants.TransformJsonFileFullPath, optional: false, reloadOnChange: true);
        }
    }
}
