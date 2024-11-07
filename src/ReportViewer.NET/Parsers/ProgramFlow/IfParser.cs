using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.ProgramFlow
{
    public class IfParser : BaseParser
    {
        public static Regex IfRegex = RegexCommon.GenerateMultiParamParserRegex("IIF");

        public IfParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, IfRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            // 1. Extract boolean expression
            var match = IfRegex.Match(this.CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding IIF including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(4, matchValue.Length - 5);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 3)
            {
                // The IIF function expects 3 parameters.
                return;
            }

            var booleanExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0], 
                this.DataSetResults, 
                this.Values, 
                this.CurrentRowNumber, 
                this.DataSets, 
                this.ActiveDataset, 
                null
            );
            
            var thenExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1], 
                this.DataSetResults, 
                this.Values, 
                this.CurrentRowNumber, 
                this.DataSets, 
                this.ActiveDataset, 
                null
            );
            
            var elseExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[2], 
                this.DataSetResults, 
                this.Values, 
                this.CurrentRowNumber, 
                this.DataSets, 
                this.ActiveDataset, 
                null
            );

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = thenExpression.GetType();
            this.CurrentExpression.Value = booleanExpression != null && (bool)booleanExpression ? thenExpression : elseExpression;
        }
    }
}
