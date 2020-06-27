using System;
using System.Globalization;

namespace DataTransform.Api.Hosting
{
    public static class SqlTypeHelper
    {
        public static string QuoteString => "\"";
        public static string DoubleQuoteString => "\"\"";
        public static string SingleQuoteString => "'";
        public static string DoubleSingleQuoteString => "''";

        public static string Quote(string value)
        {
            return SingleQuoteString + value.Replace(SingleQuoteString, DoubleSingleQuoteString) + SingleQuoteString;
        }

        public static string GetSqlValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            switch (Convert.GetTypeCode(value))
            {
                case TypeCode.Object:
                case TypeCode.String:
                case TypeCode.Char:
                    return Quote(value.ToString());
                case TypeCode.Boolean:
                    return (bool)value ? "1" : "0";
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                case TypeCode.DateTime:
                    return String.Concat("'", Convert.ToString(value, CultureInfo.InvariantCulture), "'");
            }

            return "null";
        }
    }
}
