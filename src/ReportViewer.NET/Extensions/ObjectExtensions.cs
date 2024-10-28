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

        public static int ExpressionAsInt(this object obj) 
        {
            return int.Parse(obj.ToString());
        }

        public static long ExpressionAsLong(this object obj)
        {
            return long.Parse(obj.ToString());
        }

        public static string ExpressionAsString(this object obj) 
        {
            return obj.ToString();
        }

        public static double ExpressionAsDouble(this object obj)
        {
            return double.Parse(obj.ToString(), CultureInfo.InvariantCulture);
        }

        public static decimal ExpressionAsDecimal(this object obj)
        {
            return decimal.Parse(obj.ToString(), CultureInfo.InvariantCulture);
        }
    }
}
