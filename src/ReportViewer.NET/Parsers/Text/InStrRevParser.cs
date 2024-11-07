using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Text
{
    public class InStrRevParser : BaseParser
    {
        public static Regex InStrRevRegex = RegexCommon.GenerateMultiParamParserRegex("InStrRev");

        public InStrRevParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, InStrRevRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = InStrRevRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding InStrRev including opening/closing brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(9);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count > 4)
            {
                // The InStr function expects at most 4 parameters.
                return;
            }

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(int);

            var parsedExpression = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            if (parsedExpression.IsInteger())
            {
                // We've been provided with start index.
                var startIndx = parsedExpression.ExpressionAsInt();

                var string1 = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[1],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsString();

                var string2 = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[2],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsString();

                CompareMethod compareMethod = CompareMethod.Binary;

                if (foundParameters.Item2.Count == 4)
                {
                    compareMethod = (CompareMethod)this.Report.Parser.ParseReportExpressionString(
                        foundParameters.Item2[3],
                        this.DataSetResults,
                        this.Values,
                        this.CurrentRowNumber,
                        this.DataSets,
                        this.ActiveDataset,
                        null
                    ).ExpressionAsInt();
                }

                this.CurrentExpression.Value = Strings.InStrRev(string1, string2, startIndx, compareMethod);
            }
            else
            {
                var string1 = parsedExpression.ExpressionAsString();

                var string2 = this.Report.Parser.ParseReportExpressionString(
                    foundParameters.Item2[1],
                    this.DataSetResults,
                    this.Values,
                    this.CurrentRowNumber,
                    this.DataSets,
                    this.ActiveDataset,
                    null
                ).ExpressionAsString();

                CompareMethod compareMethod = CompareMethod.Binary;

                if (foundParameters.Item2.Count == 3)
                {
                    compareMethod = (CompareMethod)this.Report.Parser.ParseReportExpressionString(
                        foundParameters.Item2[3],
                        this.DataSetResults,
                        this.Values,
                        this.CurrentRowNumber,
                        this.DataSets,
                        this.ActiveDataset,
                        null
                    ).ExpressionAsInt();
                }

                this.CurrentExpression.Value = Strings.InStrRev(string1, string2, -1, compareMethod);
            }

        }
    }
}
