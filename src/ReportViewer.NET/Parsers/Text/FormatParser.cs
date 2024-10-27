using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class FormatParser : BaseParser
    {
        public static Regex FormatRegex = RegexCommon.GenerateMultiParamParserRegex("Format");

        public FormatParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, FormatRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = FormatRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Format including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(7);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count > 2)
            {
                // The Format function expects at most 2 parameters.
                return;
            }
                        
            var parsedExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            string format = "";

            if (foundParameters.Item2.Count > 1)
            {
                format = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[1],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsString();
            }

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);

            if (!string.IsNullOrEmpty(format))
            {
                // Report Builder example is provided as =Format(Globals!ExecutionTime, "Long Date")
                // This wouldn't be accepted by C#'s DateTime format. 
                // Can't find list of non-standard formats so adding this to satisfy example.
                if (format.EqualsIgnore("Long Date"))
                {
                    format = "D";
                }

                if (format.EqualsIgnore("Short Date"))
                {
                    format = "d";
                }

                if (format.EqualsIgnore("Long Time"))
                {
                    format = "G";
                }

                if (format.EqualsIgnore("Short Time"))
                {
                    format = "g";
                }
            }

            if (parsedExpression is DateTime)
            {
                format = this.ConvertVBFormatToCSharpForDateTime(format);

                this.CurrentExpression.Value = parsedExpression.ExpressionAsDateTime().ToString(format, CultureInfo.CurrentCulture);
            }
            else if (parsedExpression is int)
            {                
                format = this.ConvertVBFormatToCSharpForNumeric(format);
                
                if (format.EqualsIgnore("yes/no"))
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsInt() == 1 ? "Yes" : "No";
                }
                else if (format.EqualsIgnore("on/off"))
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsInt() == 1 ? "On" : "Off";
                }
                else if (format.EqualsIgnore("true/false"))
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsInt() == 1 ? "True" : "False";
                }
                else
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsInt().ToString(format, CultureInfo.CurrentCulture);
                }
            }
            else if (parsedExpression is double)
            {                   
                format = this.ConvertVBFormatToCSharpForNumeric(format);

                // Handle percent symbol placement for "Percentage" format specifier.
                if (format.EqualsIgnore("Percentage"))
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsDouble().ToString("p", new NumberFormatInfo()
                    {
                        PercentPositivePattern = 1
                    });
                }
                else
                {
                    this.CurrentExpression.Value = parsedExpression.ExpressionAsDouble().ToString(format, CultureInfo.CurrentCulture);
                }                
            }
            else
            {
                this.CurrentExpression.Value = string.Format(format, parsedExpression);
            }
            
        }

        private string ConvertVBFormatToCSharpForDateTime(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return format;
            }

            switch (format.ToLower())
            {
                case "long date":
                    return "D";
                case "short date":
                    return "d";
                case "general date":
                    return "G";
                case "long time":
                case "medium time":
                    return "T";
                case "short time":
                    return "t";
            }

            return format;
        }

        private string ConvertVBFormatToCSharpForNumeric(string format)
        {
            if (string.IsNullOrEmpty(format))
            {
                return format;
            }

            switch (format.ToLower())
            {
                case "general number":
                    return "G";
                case "currency":
                    return "C";
                case "fixed":
                    return "F";
                case "standard":
                    return "N";
                case "scientific":
                    return "E2";
            }

            return format;
        }
    }
}
