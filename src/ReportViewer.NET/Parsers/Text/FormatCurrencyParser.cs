using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    // By definition, the FormatCurrency function in Report Builder will return the currency symbol as defined by the system.
    // This may result in differences if text in the report contains a specific "Format" value forcing it to a certain symbol.
    internal class FormatCurrencyParser : BaseParser
    {
        public static Regex FormatCurrencyRegex = RegexCommon.GenerateMultiParamParserRegex("FormatCurrency");

        public FormatCurrencyParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, FormatCurrencyRegex, report)
        {            
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var fcMatch = FormatCurrencyRegex.Match(CurrentString);
            var fcValue = fcMatch.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding FormatCurrency including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            fcValue = fcValue.Substring(15, fcValue.Length - 16);

            // TODO: Handle additional parameters from FormatCurrency function. For now just default to system settings.
            var commaMatches = RegexCommon.CommaNotInParenRegex.Matches(fcValue);            
            var indexes = new List<int>();

            foreach (Match commaMatch in commaMatches)
            {
                if (!ExpressionParser.WithinStringLiteral(fcValue, commaMatch.Index))
                {
                    indexes.Add(commaMatch.Index);
                }
            }

            // Let's split our string into its relevant groups.
            var stringGroups = new List<string>();
            var removed = 0;

            foreach (var index in indexes)
            {
                stringGroups.Add(fcValue.Substring(removed, index - removed));
                removed += fcValue.Substring(removed, index - removed).Length + 1;
            }

            // Then grab the last of the string.
            stringGroups.Add(fcValue.Substring(removed, fcValue.Length - removed));

            var parsedExpression = this.Report.Parser.ParseReportExpressionString(
                stringGroups[0], 
                this.DataSetResults, 
                this.Values,
                this.CurrentRowNumber,
                this.DataSets, 
                this.ActiveDataset, 
                null
            );

            this.CurrentExpression.Index = fcMatch.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = double.Parse(parsedExpression.ToString()).ToString("C");
        }
    }
}
