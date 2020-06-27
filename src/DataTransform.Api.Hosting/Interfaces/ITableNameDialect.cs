namespace DataTransform.Api.Hosting
{
    public interface ITableNameDialect
    {
        string TableName { get; set; }
        string SqlTableNameDialect();
    }
}
