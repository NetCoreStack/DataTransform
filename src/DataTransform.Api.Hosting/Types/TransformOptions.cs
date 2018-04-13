using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace DataTransform.Api.Hosting
{
    public class TransformOptions : IOptions<TransformOptions>
    {
        public string SqlConnectionString { get; set; }

        public string MongoDbConnectionString { get; set; }

        public TransformOptions Value => this;

        public IEnumerable<TransformDescriptor> Maps { get; set; }

        public TransformOptions()
        {
        }
    }
}