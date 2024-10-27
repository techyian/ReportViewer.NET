using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace ReportViewer.NET.Parsers.Text
{
    public class FormatNumberParser : BaseParser
    {
        public static Regex FormatNumberRegex = RegexCommon.GenerateMultiParamParserRegex("FormatNumber");

        public FormatNumberParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, FormatNumberRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = FormatNumberRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding FormatNumber including opening/closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(13);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count > 5)
            {
                // The FormatNumber function expects at most 5 parameters.
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

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);

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
                includeLeadingDigit = (TriState)this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[2],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsInt();
            }

            if (foundParameters.Item2.Count > 3)
            {
                // Parse UseParensForNegativeNumbers
                useParensForNegativeNumbers = (TriState)this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[3],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsInt();
            }

            if (foundParameters.Item2.Count > 4)
            {
                // Parse GroupDigits
                groupDigits = (TriState)this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[4],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsInt();
            }

            this.CurrentExpression.Value = Strings.FormatNumber(
                parsedExpression, 
                numDigitsAfterDecimal, 
                includeLeadingDigit, 
                useParensForNegativeNumbers, 
                groupDigits
            );
        }
    }
}
