using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace DataTransform.Api.Hosting
{
    public class TransformOptions : IOptions<TransformOptions>
    {
        public string SourceConnectionString { get; set; }

        public string TargetConnectionString { get; set; }

        // case insensitive index 
        // https://docs.mongodb.com/manual/core/index-case-insensitive/
        // Collation settings
        public string Locale { get; set; }

        public TransformOptions Value => this;

        public IEnumerable<TransformDescriptor> Maps { get; set; }

        public TransformOptions()
        {
        }
    }
}