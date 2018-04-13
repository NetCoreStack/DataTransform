using DataTransform.Api.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Xunit;

namespace DataTransform.Api.Tests
{
    public class TransformManagerTests : TransformTestBase
    {
        [Fact]
        public async Task TransformAsyncTests()
        {
            var transformManager = ApplicationServices.GetService<TransformManager>();
            await transformManager.TransformAsync("transform.json");
            Assert.True(true);
        }
    }
}