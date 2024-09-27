using System;

namespace ReportViewer.NET.Extensions
{
    internal static class StringExtensions
    {
        public static int IndexOfIgnore(this string str, string search)
        {
            return str.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        }
    }
}
