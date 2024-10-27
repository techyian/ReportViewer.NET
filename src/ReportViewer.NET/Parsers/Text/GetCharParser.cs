using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class GetCharParser : BaseParser
    {
        public static Regex GetCharRegex = RegexCommon.GenerateMultiParamParserRegex("GetChar");

        public GetCharParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, GetCharRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = GetCharRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding GetChar including opening/closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(8);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 2)
            {
                // The GetChar function expects 2 parameters.
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
            ).ExpressionAsString();

            var indx = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = Strings.GetChar(parsedExpression, indx);
        }
    }
}
