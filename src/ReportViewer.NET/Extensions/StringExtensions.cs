using System;

namespace ReportViewer.NET.Extensions
{
    internal static class StringExtensions
    {
        public static int IndexOfIgnore(this string str, string search)
        {
            return str.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EqualsIgnore(this string str, string search)
        {
            return str.Equals(search, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithIgnore(this string str, string search) 
        { 
            return str.StartsWith(search, StringComparison.OrdinalIgnoreCase);
        }

        public static string MatchValueSubString(this string str, int numChars)
        {
            var matchValue = str.TrimStart('(').Trim();
            
            return matchValue.Substring(numChars, matchValue.Length - (numChars + 1));
        }

        public static string TrimDatePart(this string str)
        {
            return str.Trim().Replace("\"", "");
        }
    }
}
