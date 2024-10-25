using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class MonthNameParser : BaseParser
    {
        public static Regex MonthNameRegex = RegexCommon.GenerateMultiParamParserRegex("MonthName");

        public MonthNameParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, MonthNameRegex, report)
        {            
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            int monthNum;

            var match = MonthNameRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding MonthName including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(10);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item1 == 0)
            {
                // No need to abbreviate, just return full month name.
                monthNum = (int)this.Report.Parser.ParseReportExpressionString(
                    matchValue, 
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets, 
                    this.ActiveDataset, 
                    null
                );

                CurrentExpression.Index = match.Index;
                CurrentExpression.ResolvedType = typeof(string);
                CurrentExpression.Value = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum);

                return;
            }

            // Something has gone wrong, we should have at most 1 result.
            if (foundParameters.Item1 > 1)
            {
                return;
            }

            monthNum = (int)this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets, 
                this.ActiveDataset, 
                null
            );

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(string);

            if (foundParameters.Item2.Count == 1 || foundParameters.Item2[1].Trim().EqualsIgnore("false"))
            {
                CurrentExpression.Value = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthNum);
            }
            else
            {
                CurrentExpression.Value = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(monthNum);
            }
        }
    }
}
