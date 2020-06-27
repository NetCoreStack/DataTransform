using DataTransform.Api.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
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
        public TransformTestBase()
        {
            IServiceCollection services = new ServiceCollection();
            var currentDirectory = Directory.GetCurrentDirectory();
            var wwwPath = Path.Combine(SolutionPathUtility.GetProjectPath("src", "DataTransform.Api.Hosting"), @"wwwroot");

            var mockHosting = new Mock<IWebHostEnvironment>();
            mockHosting.Setup(env => env.ApplicationName).Returns("DataTransform.Api.Tests");
            mockHosting.Setup(env => env.WebRootPath).Returns(wwwPath);
            mockHosting.Setup(env => env.ContentRootPath).Returns(currentDirectory);
            mockHosting.Setup(env => env.EnvironmentName).Returns(Environments.Development);
            services.AddSingleton<IWebHostEnvironment>(mockHosting.Object);
            services.AddSingleton<IHostEnvironment>(mockHosting.Object);

            var mock = new Mock<IConnectionManager>();
            mock.Setup(manager => manager.BroadcastAsync(It.IsAny<WebSocketMessageContext>())).Returns(Task.CompletedTask);

            var loggerFactory = new LoggerFactory();
            services.AddSingleton(loggerFactory);
            
            var builder = new ConfigurationBuilder();

            var config = builder.Build();

            services.AddSingleton<IConnectionManager>(mock.Object);

            services.AddTransformFeatures();

            ApplicationServices = services.BuildServiceProvider();
        }
    }
}