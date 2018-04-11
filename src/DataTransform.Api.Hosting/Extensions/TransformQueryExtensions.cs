using System.Linq;

namespace DataTransform.Api.Hosting
{
    public static class TransformQueryExtensions
    {
        public static string CreateSqlScript(this TransformDescriptor descriptor, out string fieldsPattern)
        {
            fieldsPattern = string.Empty;
            if (descriptor.Fields != null && descriptor.Fields.Any())
            {
                fieldsPattern = string.Join(", ", descriptor.Fields);
            }
            else
            {
                fieldsPattern = "*";
            }

            return string.Format("SELECT {0} FROM {1}", fieldsPattern, descriptor.TableName);
        }

        public static string CreateCountScript(this TransformDescriptor descriptor)
        {
            return string.Format("SELECT COUNT(*) FROM {0}", descriptor.TableName);
        }
    }
}