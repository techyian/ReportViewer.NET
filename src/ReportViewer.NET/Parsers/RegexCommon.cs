using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    internal class RegexCommon
    {
        // Credit to: https://stackoverflow.com/a/39565427
        public static Regex CommaNotInParenRegex = new Regex(",(?![^(]*\\))");

        // Credit to: https://stackoverflow.com/a/23667311
        public static Regex TextInQuotesRegex = new Regex("\\\\\"|\"(?:\\\\\"|[^\"])*\"|(\\+)");

        public static Regex GenerateMultiParamParserRegex(string parserName)
        {
            // Credit to: https://stackoverflow.com/a/35271017
            // Ensures correct number of opening/closing braces are respected.
            var pattern = "(?:\\(*?)(?i:PARSERNAME?)\\((?>\\((?<c>)|[^()]+|\\)(?<-c>))*(?(c)(?!))\\)".Replace("PARSERNAME", parserName);
            
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        // Credit to: https://stackoverflow.com/a/76614259
        //public static Regex InnerMemberRegex = new Regex("[A-Za-z]+\\s*\\([0-9\\+\\*\\-\\s]+\\)");
    }
}
