using System.Collections.Generic;

namespace DataTransform.Api.Hosting
{
    public class TransformDescriptor
    {
        public string TableName { get; set; }
        public string IdentityColumnName { get; set; }
        public string CollectionName { get; set; }
        public IEnumerable<long> Range { get; set; }
        public IEnumerable<string> Fields { get; set; }
    }
}
