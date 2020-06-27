using DataTransform.Api.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCoreStack.WebSockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DataTransform.Api.Tests
{
    public class TransformManagerTests : TransformTestBase
    {
        [Fact]
        public async Task TransformSql2MongoDbAsyncTests()
        {
            var transformManager = ApplicationServices.GetService<TransformManager>();
            await transformManager.TransformAsync(new[] { "musicstoreSql2Mongo.json" }, CancellationToken.None);
            Assert.True(true);
        }

        [Fact]
        public async Task TransformSql2SqlDbAsyncTests()
        {
            var transformManager = ApplicationServices.GetService<TransformManager>();
            await transformManager.TransformAsync(new[] { "musicstoreSql2Sql.json" }, CancellationToken.None);
            Assert.True(true);
        }

        [Fact]
        public async Task TransformStateSql2SqlDbAsyncTests()
        {
            var connectionManager = ApplicationServices.GetService<IConnectionManager>();
            var hostingEnvironment = ApplicationServices.GetService<IWebHostEnvironment>();
            var configFilePath = Path.Combine(hostingEnvironment.WebRootPath, "configs", "StateSql2SqlTest.json");
            var configuration = new ConfigurationBuilder().AddJsonFile(configFilePath).Build();
            var options = new TransformOptions();
            configuration.Bind(nameof(TransformOptions), options);


            var sqlTransformTask = new SqlTransformTask(options, connectionManager, CancellationToken.None);

            var context = options.CreateSqlTransformContexts(CancellationToken.None);

            await sqlTransformTask.InvokeAsync(context.ToArray());

            Assert.True(true);
        }
    }
}