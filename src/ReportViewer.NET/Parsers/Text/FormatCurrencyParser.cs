using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    // By definition, the FormatCurrency function in Report Builder will return the currency symbol as defined by the system.
    // This may result in differences if text in the report contains a specific "Format" value forcing it to a certain symbol.
    internal class FormatCurrencyParser : BaseParser
    {
        public static Regex FormatCurrencyRegex = RegexCommon.GenerateMultiParamParserRegex("FormatCurrency");

        public FormatCurrencyParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, FormatCurrencyRegex, report)
        {            
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var fcMatch = FormatCurrencyRegex.Match(this.CurrentString);
            var fcValue = fcMatch.Value;

            // Remove the surrounding FormatCurrency including closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            fcValue = fcValue.MatchValueSubString(15);

            var foundParameters = this.ParseParenthesis(fcValue);

            if (foundParameters.Item2.Count > 5)
            {
                // The FormatCurrency function expects at most 5 parameters.
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

            this.CurrentExpression.Index = fcMatch.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = Strings.FormatCurrency(
                parsedExpression, 
                numDigitsAfterDecimal, 
                includeLeadingDigit, 
                useParensForNegativeNumbers, 
                groupDigits
            );
        }
    }
}
