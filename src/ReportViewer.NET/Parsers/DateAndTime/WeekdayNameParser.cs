using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class WeekdayNameParser : BaseParser
    {
        public static Regex WeekdayNameRegex = RegexCommon.GenerateMultiParamParserRegex("WeekdayName");

        public WeekdayNameParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, WeekdayNameRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = WeekdayNameRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding WeekdayName including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(12);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count < 1 || foundParameters.Item2.Count > 3)
            {
                // The Weekday function expects at least 1 parameter but no more than 3.
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

            FirstDayOfWeek fdow = FirstDayOfWeek.System;

            if (foundParameters.Item2.Count == 3)
            {
                var fdowString = foundParameters.Item2[2].Trim();

                if (int.TryParse(fdowString, out var fdowInt))
                {
                    fdow = (FirstDayOfWeek)fdowInt;
                }
                else
                {
                    fdow = (FirstDayOfWeek)Enum.Parse(typeof(FirstDayOfWeek), fdowString.Substring(15, fdowString.Length - 15));
                }
            }

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);

            if (foundParameters.Item2.Count == 1 || foundParameters.Item2[1].Trim().EqualsIgnore("false"))
            {
                this.CurrentExpression.Value = date.ParseWeekdayName(fdow, false);
            }
            else
            {
                this.CurrentExpression.Value = date.ParseWeekdayName(fdow, true);
            }            
        }
    }
}
