using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class FormatPercentParser : BaseParser
    {
        public static Regex FormatPercentRegex = RegexCommon.GenerateMultiParamParserRegex("FormatPercent");

        public FormatPercentParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, FormatPercentRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = FormatPercentRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding FormatPercent including opening/closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(14);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count > 5)
            {
                // The FormatPercent function expects at most 5 parameters.
                return;
            }

            var parsedExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            int numDigitsAfterDecimal = -1;
            TriState includeLeadingDigit = TriState.UseDefault;
            TriState useParensForNegativeNumbers = TriState.UseDefault;
            TriState groupDigits = TriState.UseDefault;

            if (foundParameters.Item2.Count > 1)
            {
                // Parse NumDigitsAfterDecimal
                numDigitsAfterDecimal = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[1],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsInt();
            }

            if (foundParameters.Item2.Count > 2)
            {
                // Parse IncludeLeadingDigit
                var expr = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[2],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                );

                if (int.TryParse(expr.ToString(), out var parsed))
                {
                    includeLeadingDigit = (TriState)parsed;
                }
                else
                {
                    includeLeadingDigit = (TriState)Enum.Parse(typeof(TriState), expr.ToString());
                }
            }

            if (foundParameters.Item2.Count > 3)
            {
                // Parse UseParensForNegativeNumbers
                var expr = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[3],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                );

                if (int.TryParse(expr.ToString(), out var parsed))
                {
                    useParensForNegativeNumbers = (TriState)parsed;
                }
                else
                {
                    useParensForNegativeNumbers = (TriState)Enum.Parse(typeof(TriState), expr.ToString());
                }
            }

            if (foundParameters.Item2.Count > 4)
            {
                // Parse GroupDigits
                var expr = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[4],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                );

                if (int.TryParse(expr.ToString(), out var parsed))
                {
                    groupDigits = (TriState)parsed;
                }
                else
                {
                    groupDigits = (TriState)Enum.Parse(typeof(TriState), expr.ToString());
                }
            }

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = Strings.FormatPercent(
                parsedExpression,
                numDigitsAfterDecimal,
                includeLeadingDigit,
                useParensForNegativeNumbers,
                groupDigits
            );
        }
    }
}
