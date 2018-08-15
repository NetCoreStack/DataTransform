using DataTransform.Api.Hosting;
using Microsoft.Extensions.DependencyInjection;
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
    }
}