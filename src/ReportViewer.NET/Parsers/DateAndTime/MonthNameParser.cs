using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
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
            TablixOperator op,
            TablixExpression currentExpression,
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
            var monthNum = 1;
            var match = MonthNameRegex.Match(CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding MonthName including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(10, matchValue.Length - 11);

            var commaMatches = RegexCommon.CommaNotInParenRegex.Matches(matchValue);
            var indexes = new List<int>();

            if (commaMatches.Count == 0)
            {
                // No need to abbreviate, just return full month name.
                monthNum = (int)this.Report.Parser.ParseTablixExpressionString(
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

            // We've got more than we were looking for. So we must now look at commas found within quotes and strip out the ones we don't want.
            // This probably isn't the most performant way of doing this...
            foreach (Match commaMatch in commaMatches)
            {
                if (!ExpressionParser.WithinStringLiteral(matchValue, commaMatch.Index))
                {
                    indexes.Add(commaMatch.Index);
                }
            }

            // Something has gone wrong, we should have at most 1 result.
            if (indexes.Count > 1)
            {
                return;
            }

            // Let's split our string into its relevant groups.
            var stringGroups = new List<string>();
            var removed = 0;

            foreach (var index in indexes)
            {
                stringGroups.Add(matchValue.Substring(removed, index - removed));
                removed += matchValue.Substring(removed, index - removed).Length + 1;
            }

            // Then grab the last of the string.
            stringGroups.Add(matchValue.Substring(removed, matchValue.Length - removed));

            monthNum = (int)this.Report.Parser.ParseTablixExpressionString(
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

            if (stringGroups.Count == 1 || stringGroups[1].ToLower() == "false")
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
