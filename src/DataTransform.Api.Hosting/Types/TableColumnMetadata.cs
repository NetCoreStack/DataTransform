using System;

namespace DataTransform.Api.Hosting
{
    public class TableColumnMetadata
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public int MaxLength { get; set; }
        public bool Nullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public Type ClrType => SqlQueryHelper.GetClrType(DataType);
    }
}
