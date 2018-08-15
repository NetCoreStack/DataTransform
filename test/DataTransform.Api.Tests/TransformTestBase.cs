using DataTransform.Api.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NetCoreStack.WebSockets;
using System;
using System.IO;
using System.Threading.Tasks;

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

            var mock = new Mock<IConnectionManager>();
            mock.Setup(manager => manager.BroadcastAsync(It.IsAny<WebSocketMessageContext>())).Returns(Task.CompletedTask);

            var loggerFactory = new LoggerFactory();
            services.AddSingleton(loggerFactory);
            
            var builder = new ConfigurationBuilder();

            var config = builder.Build();

            services.AddSingleton<IConnectionManager>(mock.Object);
            services.AddSingleton(HostingEnvironment);

            services.AddTransformFeatures();

            ApplicationServices = services.BuildServiceProvider();
        }
    }
}