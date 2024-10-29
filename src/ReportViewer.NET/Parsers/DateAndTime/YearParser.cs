using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class YearParser : BaseParser
    {
        public static Regex YearRegex = RegexCommon.GenerateMultiParamParserRegex("Year");

        public YearParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, YearRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = YearRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Day including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(5);

            var dtt = this.Report.Parser.ParseReportExpressionString(
                matchValue,
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(int);
            CurrentExpression.Value = dtt.Year;
        }
    }
}
