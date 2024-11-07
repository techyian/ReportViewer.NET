using System;
using System.Globalization;

namespace ReportViewer.NET.Extensions
{
    public static class ObjectExtensions
    {
        public static DateTime ExpressionAsDateTime(this object obj)
        {
            if (obj is string)
            {
                return DateTime.Parse(obj.ToString(), CultureInfo.CurrentCulture);
            }

            if (obj is DateTime)
            {
                return (DateTime)obj;
            }

            return DateTime.MinValue;
        }

        public static bool IsInteger(this object obj)
        {
            return obj is int || obj is short || obj is long;
        }

        public static int ExpressionAsInt(this object obj) 
        {
            return int.Parse(obj.ExpressionAsString());
        }

        public static long ExpressionAsLong(this object obj)
        {
            return long.Parse(obj.ExpressionAsString());
        }

        public static string ExpressionAsString(this object obj) 
        {
            return Convert.ToString(obj, CultureInfo.InvariantCulture);
        }

        public static double ExpressionAsDouble(this object obj)
        {            
            return double.Parse(obj.ExpressionAsString(), NumberStyles.Number, CultureInfo.InvariantCulture);
        }

        public static decimal ExpressionAsDecimal(this object obj)
        {
            return decimal.Parse(obj.ExpressionAsString(), NumberStyles.Number, CultureInfo.InvariantCulture);
        }
    }
}
