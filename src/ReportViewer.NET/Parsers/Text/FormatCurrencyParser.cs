using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    internal class FormatCurrencyParser : BaseParser
    {
        public static Regex FormatCurrencyRegex = new Regex("(?:\\(*?)(?i:FormatCurrency?)\\((?>\\((?<c>)|[^()]+|\\)(?<-c>))*(?(c)(?!))\\)", RegexOptions.IgnoreCase);
        
        private readonly ExpressionParser _expressionParser;

        public FormatCurrencyParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, FormatCurrencyRegex)
        {
            _expressionParser = new ExpressionParser();
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

            var parsedExpression = _expressionParser.ParseTablixExpressionString(fcValue, this.DataSetResults, this.Values, this.DataSets, this.ActiveDataset, null);

            this.CurrentExpression.Index = fcMatch.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = double.Parse(parsedExpression.ToString()).ToString("C");
        }
    }
}
