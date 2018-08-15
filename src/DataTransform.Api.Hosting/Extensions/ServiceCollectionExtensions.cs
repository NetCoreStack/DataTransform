using Microsoft.Extensions.DependencyInjection;

namespace DataTransform.Api.Hosting
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTransformFeatures(this IServiceCollection services)
        {
            services.AddOptions();

            services.AddSingleton<TransformTaskFactory>();
            services.AddSingleton<TransformManager>();
        }
    }
}
