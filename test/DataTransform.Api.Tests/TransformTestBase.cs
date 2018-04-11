using DataTransform.Api.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DataTransform.Api.Tests
{
    public class TransformTestBase
    {
        public IServiceProvider ApplicationServices { get; }
        public IHostingEnvironment HostingEnvironment { get; }
        public TransformTestBase()
        {
            IServiceCollection services = new ServiceCollection();
            var currentDirectory = Directory.GetCurrentDirectory();
            var wwwPath = Path.Combine(SolutionPathUtility.GetProjectPath("src", "DataTransform.Api.Hosting"), @"wwwroot");

            HostingEnvironment = new HostingEnvironment
            {
                WebRootPath = wwwPath,
                ContentRootPath = currentDirectory,
                EnvironmentName = EnvironmentName.Development
            };

            var loggerFactory = new LoggerFactory();
            services.AddSingleton(loggerFactory);
            
            var builder = new ConfigurationBuilder();
            builder.AddTransformConfigFile(HostingEnvironment);

            var config = builder.Build();

            services.AddTransformFeatures(config);

            ApplicationServices = services.BuildServiceProvider();
        }
    }
}