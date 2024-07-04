using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class IsNothingParser : BaseParser
    {
        public static Regex IsNothingRegex = new Regex("(?:\\(*?)(?i:IsNothing?)\\((?>\\((?<c>)|[^()]+|\\)(?<-c>))*(?(c)(?!))\\)", RegexOptions.IgnoreCase);

        private ExpressionParser _expressionParser;

        public IsNothingParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, IsNothingRegex)
        {
            _expressionParser = new ExpressionParser();
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = IsNothingRegex.Match(this.CurrentString);
            var expression = this.CurrentString.Substring(10, this.CurrentString.Length - 11);

            var resolvedValue = _expressionParser.ParseTablixExpressionString(expression, this.DataSetResults, this.Values, this.DataSets, null);

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(bool);
            this.CurrentExpression.Value = resolvedValue != null && !string.IsNullOrEmpty(((object)resolvedValue).ToString());
        }
    }
}
