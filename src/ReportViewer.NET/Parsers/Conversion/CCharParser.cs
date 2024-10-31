using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Conversion
{
    public class CCharParser : BaseParser
    {
        public static Regex CCharRegex = RegexCommon.GenerateMultiParamParserRegex("CChar");

        public CCharParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, CCharRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = CCharRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding CChar including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(6);

            var expr = this.Report.Parser.ParseReportExpressionString(
                matchValue,
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            );

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(char);
            this.CurrentExpression.Value = Convert.ToChar(expr);
        }
    }
}
