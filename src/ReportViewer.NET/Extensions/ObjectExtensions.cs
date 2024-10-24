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

        public static string ExpressionAsString(this object obj) 
        {
            return obj.ToString();
        }
    }
}
