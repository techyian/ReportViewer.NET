using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class FormatDateTimeParser : BaseParser
    {
        public static Regex DateFormatRegex = RegexCommon.GenerateMultiParamParserRegex("FormatDateTime");

        public FormatDateTimeParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DateFormatRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DateFormatRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding FormatDateTime including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(15);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 2)
            {
                // The FormatDateTime function expects 2 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.
            var date = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            foundParameters.Item2[1] = foundParameters.Item2[1].Trim();

            var dateFormatStrPart = foundParameters.Item2[1].Substring(11, foundParameters.Item2[1].Length - 11);
            var dateFormat = (DateFormat)Enum.Parse(typeof(DateFormat), dateFormatStrPart);

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = date.FormatDateTime(dateFormat);
        }
    }
}
