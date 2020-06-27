namespace DataTransform.Api.Hosting
{
    public class TableNameDialect : ITableNameDialect
    {
        public string TableName { get; set; }

        public virtual string SqlTableNameDialect()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                return string.Empty;
            }

            var names = TableName.Split('.');
            if (names.Length <= 1)
            {
                return string.Empty;
            }

            var scheme = names[0];
            var tableName = names[1];

            return $"[{scheme}].[{tableName}]";
        }
    }
}
