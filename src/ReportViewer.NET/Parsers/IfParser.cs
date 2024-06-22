using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class IfParser : BaseParser
    {
        // Credit to: https://stackoverflow.com/a/35271017
        // Ensures correct number of opening/closing braces are respected.
        public static Regex IfRegex = new Regex("(?:\\(*?)(?i:IIF?)\\((?>\\((?<c>)|[^()]+|\\)(?<-c>))*(?(c)(?!))\\)");

        private ExpressionParser _expressionParser;

        public IfParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, IfRegex)
        {
            _expressionParser = new ExpressionParser();
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            // 1. Extract boolean expression
            var match = IfRegex.Match(this.CurrentString);
            
            var elseValueIdx = this.CurrentString.LastIndexOf(',');
            var elseValue = this.CurrentString.Substring(elseValueIdx + 1, this.CurrentString.Length - elseValueIdx - 1).TrimEnd(')').Replace("\"", "");
            var remaining = this.CurrentString.Substring(0, elseValueIdx);
            var ifValueIdx = remaining.LastIndexOf(',');
            var ifValue = remaining.Substring(ifValueIdx + 1, remaining.Length - ifValueIdx - 1).Replace("\"", "");
            
            // Start from idx 4 to remove IIF(
            remaining = remaining.Substring(4, ifValueIdx - 4);

            var booleanExpression = _expressionParser.ParseTablixExpressionString(remaining, this.DataSetResults, this.Values, this.DataSets, null);

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = (bool)booleanExpression ? ifValue : elseValue;
        }
    }
}
