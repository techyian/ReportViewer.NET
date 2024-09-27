using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class LeftParser : BaseParser
    {
        public static Regex LeftRegex = RegexCommon.GenerateMultiParamParserRegex("Left");

        private ExpressionParser _expressionParser;

        public LeftParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, LeftRegex, report)
        {
            _expressionParser = new ExpressionParser(report);
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = LeftRegex.Match(CurrentString);
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");

            // Remove the surrounding Left including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.Substring(5, matchValue.Length - 6);

            var commaMatches = RegexCommon.CommaNotInParenRegex.Matches(matchValue);
            //var textInQuotesMatches = TextInQuotesRegex.Matches(matchValue);
            var indexes = new List<int>();

            if (commaMatches.Count == 0)
            {
                // This has gone wrong.
                return;
            }

            // We want to end up with 1 comma resulting in 2 groups. String content/num chars.
            if (commaMatches.Count == 1)
            {
                // Great, we've found what we're looking for straight away.
                indexes.AddRange(commaMatches.Select(m => m.Index));
            }
            else
            {
                // We've got more than we were looking for. So we must now look at commas found within quotes and strip out the ones we don't want.
                // This probably isn't the most performant way of doing this...
                foreach (Match commaMatch in commaMatches)
                {
                    if (!ExpressionParser.WithinStringLiteral(matchValue, commaMatch.Index))
                    {
                        indexes.Add(commaMatch.Index);
                    }
                }
            }

            // We should now have our match. If not, something has gone wrong.
            if (indexes.Count != 1)
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

            var stringExpression = (string)_expressionParser.ParseTablixExpressionString(stringGroups[0], DataSetResults, Values, DataSets, ActiveDataset, null);
            var numChars = int.Parse(stringGroups[1]);

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(string);
            CurrentExpression.Value = numChars > stringExpression.Length ? stringExpression : stringExpression.Substring(0, numChars);
        }
    }
}
