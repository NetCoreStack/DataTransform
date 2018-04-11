namespace DataTransform.SharedLibrary
{
    public abstract class DbTransformContextBase
    {
        public string SelectSql { get; set; }
        public string CountSql { get; set; }
        public string FieldPattern { get; set; }
        public string TableName { get; set; }
        public object LastIndexId { get; set; }
        public string CollectionName { get; set; }
        public string IdentityColumnName { get; set; }
        public int BundleSize { get; set; } = 2000;
    }
}
