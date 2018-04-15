using MongoDB.Driver;

namespace DataTransform.Api.Hosting
{
    public class DbTransformContext : DbTransformContextBase
    {
        public Collation Collation { get; set; }
    }
}
