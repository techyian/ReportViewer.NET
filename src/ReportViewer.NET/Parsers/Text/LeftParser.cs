using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class LeftParser : BaseParser
    {
        public static Regex LeftRegex = RegexCommon.GenerateMultiParamParserRegex("Left");
                
        public LeftParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, LeftRegex, report)
        {        
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = LeftRegex.Match(CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding Left including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(5);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 2)
            {
                // The Left function expects 2 parameters.
                return;
            }

            var stringExpression = (string)this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0], 
                this.DataSetResults, 
                this.Values,
                this.CurrentRowNumber, 
                this.DataSets, 
                this.ActiveDataset, 
                null
            );

            var numChars = int.Parse(foundParameters.Item2[1]);

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(string);
            CurrentExpression.Value = numChars > stringExpression.Length ? stringExpression : stringExpression.Substring(0, numChars);
        }
    }
}
