using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class TimeSerialParser : BaseParser
    {
        public static Regex TimeSerialRegex = RegexCommon.GenerateMultiParamParserRegex("TimeSerial");

        public TimeSerialParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, TimeSerialRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = TimeSerialRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding TimeSerial including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(11, matchValue.Length - 12);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 3)
            {
                // The TimeSerial function expects 3 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.
            var hour = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            var minute = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            var second = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[2],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(DateTime);
            this.CurrentExpression.Value = new DateTime(1, 1, 1, hour, minute, second);
        }
    }
}
