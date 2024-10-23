using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class DatePartParser : BaseParser
    {
        public static Regex DatePartRegex = RegexCommon.GenerateMultiParamParserRegex("DatePart");

        public DatePartParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DatePartRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DatePartRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding DatePart including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(9);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count < 2)
            {
                // The DatePart function expects at least 2 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.
            var date = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[1],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            var datepart = foundParameters.Item2[0].TrimDatePart();
            DateInterval dateInterval;
            FirstDayOfWeek fdow = FirstDayOfWeek.System;
            FirstWeekOfYear fwoy = FirstWeekOfYear.System;

            int datePart;

            if (foundParameters.Item2.Count >= 3)
            {
                var fdowString = foundParameters.Item2[2].Trim();

                fdow = (FirstDayOfWeek)Enum.Parse(typeof(FirstDayOfWeek), fdowString.Substring(15, fdowString.Length - 15));
            }

            if (foundParameters.Item2.Count == 4)
            {
                var fwoyString = foundParameters.Item2[3].Trim();

                fwoy = (FirstWeekOfYear)Enum.Parse(typeof(FirstWeekOfYear), fwoyString.Substring(16, fwoyString.Length - 16));
            }

            if (datepart.StartsWithIgnore("DateInterval."))
            {
                dateInterval = (DateInterval)Enum.Parse(typeof(DateInterval), datepart.Substring(13, datepart.Length - 13));

                datePart = date.ParseDatePartInterval(dateInterval, fdow, fwoy);
            }
            else
            {
                datePart = date.ParseDatePartShortString(datepart, fdow, fwoy);
            }

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(int);
            this.CurrentExpression.Value = datePart;
        }
    }
}
