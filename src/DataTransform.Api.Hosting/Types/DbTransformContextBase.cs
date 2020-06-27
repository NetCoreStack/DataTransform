using System.Threading;

namespace DataTransform.Api.Hosting
{
    public abstract class DbTransformContextBase : TableNameDialect
    {
        public string SelectSql { get; set; }
        public string CountSql { get; set; }
        public string FieldPattern { get; set; }        
        public object LastIndexId { get; set; }
        public string IdentityColumnName { get; set; }
        public int BundleSize { get; set; } = 1000;
        public long Count { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
