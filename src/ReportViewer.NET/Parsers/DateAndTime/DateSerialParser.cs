using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class DateSerialParser : BaseParser
    {
        public static Regex DateSerialRegex = RegexCommon.GenerateMultiParamParserRegex("DateSerial");

        public DateSerialParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DateSerialRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DateSerialRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding DateSerial including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(11);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 3)
            {
                // The DateSerial function expects 3 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.
            var year = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            var month = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            var day = this.Report.Parser.ParseReportExpressionString(
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
            this.CurrentExpression.Value = new DateTime(year, month, day, 0, 0, 0);
        }
    }
}
