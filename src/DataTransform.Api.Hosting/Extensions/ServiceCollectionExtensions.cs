using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NetCoreStack.Data;

namespace DataTransform.Api.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTransformFeatures(this IServiceCollection services)
        {
            services.AddOptions();

            services.TryAddSingleton<ICollectionNameSelector, DefaultCollectionNameSelector>();

            services.AddSingleton<TransformTaskFactory>();

            services.AddSingleton<TransformManager>();
        }
    }
}
