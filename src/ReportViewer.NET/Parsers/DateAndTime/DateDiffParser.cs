using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class DateDiffParser : BaseParser
    {
        public static Regex DateDiffRegex = RegexCommon.GenerateMultiParamParserRegex("DateDiff");

        public DateDiffParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DateDiffRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DateDiffRegex.Match(CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding DateDiff including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(9, matchValue.Length - 10);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count < 3)
            {
                // The DateDiff function expects at least 3 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.            
            var date1 = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            var date2 = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[2],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            var datepart = foundParameters.Item2[0].Replace("\"", "");
            long difference;
            DateInterval dateInterval;

            if (datepart.StartsWithIgnore("DateInterval."))
            {
                dateInterval = (DateInterval)Enum.Parse(typeof(DateInterval), datepart.Substring(13, datepart.Length - 13));

                difference = date1.ParseDateDiff(dateInterval, date2);
            }
            else
            {
                difference = date1.ParseDateDiffShortString(datepart, date2);
            }

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(long);
            CurrentExpression.Value = difference;
        }
    }
}
