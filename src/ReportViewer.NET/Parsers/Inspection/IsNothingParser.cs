using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Inspection
{
    public class IsNothingParser : BaseParser
    {
        public static Regex IsNothingRegex = RegexCommon.GenerateMultiParamParserRegex("IsNothing");

        private ExpressionParser _expressionParser;

        public IsNothingParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, IsNothingRegex)
        {
            _expressionParser = new ExpressionParser();
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = IsNothingRegex.Match(CurrentString);
            var expression = CurrentString.Substring(10, CurrentString.Length - 11);

            var resolvedValue = _expressionParser.ParseTablixExpressionString(expression, DataSetResults, Values, DataSets, ActiveDataset, null);

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(bool);
            CurrentExpression.Value = resolvedValue != null && !string.IsNullOrEmpty(resolvedValue.ToString());
        }
    }
}
