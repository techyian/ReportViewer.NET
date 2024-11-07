using Microsoft.VisualBasic;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Math
{
    public class RoundParser : BaseParser
    {
        public static Regex RoundRegex = RegexCommon.GenerateMultiParamParserRegex("Round");

        public RoundParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, RoundRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = RoundRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Round including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(6);

            var foundParameters = this.ParseParenthesis(matchValue);

            if (foundParameters.Item2.Count < 1 || foundParameters.Item2.Count > 2)
            {
                // The DatePart function expects at most 2 parameters.
                return;
            }

            var expr = this.Report.Parser.ParseReportExpressionString(
                foundParameters.Item2[0],
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            MidpointRounding mode = MidpointRounding.ToEven;

            if (foundParameters.Item2.Count > 1)
            {
                var mprString = foundParameters.Item2[1].Trim();

                if (int.TryParse(mprString, out var mprInt))
                {
                    mode = (MidpointRounding)mprInt;
                }
                else
                {                    
                    var startIdx = 0;
                    if (mprString.StartsWith("System"))
                    {
                        startIdx = 24;
                    }
                    else
                    {
                        startIdx = 17;
                    }

                    mode = (MidpointRounding)Enum.Parse(typeof(MidpointRounding), mprString.Substring(startIdx, mprString.Length - startIdx));
                }
            }

            this.CurrentExpression.Index = match.Index;

            if (expr.IsInteger())
            {
                this.CurrentExpression.ResolvedType = typeof(long);
                this.CurrentExpression.Value = Convert.ToInt64(System.Math.Round(Convert.ToDouble(expr), mode));
            }
            else if (expr is decimal)
            {
                this.CurrentExpression.ResolvedType = typeof(long);
                this.CurrentExpression.Value = Convert.ToInt64(System.Math.Round((decimal)expr, mode));
            }
            else if (expr is double)
            {
                this.CurrentExpression.ResolvedType = typeof(long);
                this.CurrentExpression.Value = Convert.ToInt64(System.Math.Round((double)expr, mode));
            }
        }
    }
}
