using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Aggregate
{
    public class SumParser : BaseParser
    {
        public static Regex SumRegex = RegexCommon.GenerateMultiParamParserRegex("Sum");

        public SumParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, SumRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string matchValue, string dataSetName)
        {
            IEnumerable<IDictionary<string, object>> results = null;
            DataSet activeDataset = null;

            if (!string.IsNullOrEmpty(dataSetName))
            {
                var dataSet = DataSets.FirstOrDefault(ds => ds.Name == dataSetName);
                activeDataset = dataSet;
                results = dataSet.DataSetResults;
            }
            else
            {
                activeDataset = this.ActiveDataset;
                results = DataSetResults;
            }

            if (results != null && results.Count() > 0)
            {
                var first = this.Report.Parser.ParseReportExpressionString(
                    matchValue,
                    results,
                    results.First(),
                    this.CurrentRowNumber,
                    this.DataSets,
                    activeDataset,
                    null
                );

                if (first is decimal)
                {
                    decimal total = 0;

                    total += (decimal)first;

                    for (var i = 1; i < results.Count(); i++)
                    {
                        total += this.Report.Parser.ParseReportExpressionString(
                            matchValue,
                            results,
                            results.ElementAt(i),
                            this.CurrentRowNumber,
                            this.DataSets,
                            activeDataset,
                            null
                        ).ExpressionAsDecimal();
                    }

                    return (typeof(decimal), total);
                }
                else if (first is double)
                {
                    double total = 0;

                    total += (double)first;

                    for (var i = 1; i < results.Count(); i++)
                    {
                        total += this.Report.Parser.ParseReportExpressionString(
                            matchValue,
                            results,
                            results.ElementAt(i),
                            this.CurrentRowNumber,
                            this.DataSets,
                            activeDataset,
                            null
                        ).ExpressionAsDouble();
                    }

                    return (typeof(double), total);
                }                
                else
                {
                    // Fallback and parse on long.
                    long total = 0;

                    total += first.ExpressionAsLong();

                    for (var i = 1; i < results.Count(); i++)
                    {
                        total += this.Report.Parser.ParseReportExpressionString(
                            matchValue,
                            results,
                            results.ElementAt(i),
                            this.CurrentRowNumber,
                            this.DataSets,
                            activeDataset,
                            null
                        ).ExpressionAsLong();
                    }

                    return (typeof(long), total);
                }
            }

            return (typeof(double), 0);
        }
                
        public override void Parse()
        {            
            var match = SumRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Sum including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(4);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count < 1 || foundParameters.Item2.Count > 2)
            {
                // The Sum function expects at least 1 parameters but no more than 2.
                return;
            }

            if (foundParameters.Item2.Count == 1)
            {
                // Use main dataset.
                (Type, object) extractedValue = ExtractExpressionValue(matchValue, CurrentExpression.DataSetName);

                CurrentExpression.ResolvedType = extractedValue.Item1;
                CurrentExpression.Value = extractedValue.Item2;
            }
            else
            {
                // Find specific dataset.
                (Type, object) extractedValue = ExtractExpressionValue(matchValue, foundParameters.Item2[1]);

                CurrentExpression.ResolvedType = extractedValue.Item1;
                CurrentExpression.Value = extractedValue.Item2;
            }
        }
    }
}
