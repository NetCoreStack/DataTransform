using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace DataTransform.Api.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, builder) =>
            {
                // builder.AddTransformConfigFile(context.HostingEnvironment);

            }).UseStartup<Startup>().Build();
    }
}
