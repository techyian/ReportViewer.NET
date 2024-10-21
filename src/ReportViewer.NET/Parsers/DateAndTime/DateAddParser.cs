using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class DateAddParser : BaseParser
    {
        public static Regex DateAddRegex = RegexCommon.GenerateMultiParamParserRegex("DateAdd");

        public DateAddParser(
            string currentString, 
            ExpressionFieldOperator op, 
            ReportExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            int currentRowNumber, 
            IEnumerable<DataSet> dataSets, 
            DataSet activeDataset,             
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DateAddRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DateAddRegex.Match(CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding DateAdd including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(8, matchValue.Length - 9);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count != 3)
            {
                // The DateAdd function expects 3 parameters.
                return;
            }

            // DateTime will either come directly from database or will be calculated from other expression.
            var date = (DateTime)this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[2],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            var datepart = foundParameters.Item2[0].Replace("\"", "");
            var increment = int.Parse(foundParameters.Item2[1]);
            DateInterval dateInterval;

            if (datepart.StartsWithIgnore("DateInterval."))
            {
                dateInterval = (DateInterval)Enum.Parse(typeof(DateInterval), datepart.Substring(13, datepart.Length - 13));

                date = date.ParseDateInterval(dateInterval, increment);
            }
            else
            {
                date = date.ParseDateIntervalShortString(datepart, increment);
            }

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(DateTime);
            CurrentExpression.Value = date;
        }
    }
}
