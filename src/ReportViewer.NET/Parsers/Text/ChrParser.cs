using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;
using ReportViewer.NET.Extensions;
using Microsoft.VisualBasic;

namespace ReportViewer.NET.Parsers.Text
{
    public class ChrParser : BaseParser
    {
        public static Regex ChrRegex = RegexCommon.GenerateParserRegex("Chr");

        public ChrParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ChrRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = ChrRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Chr including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(4);

            var code = this.Report.Parser.ParseReportExpressionString(
                matchValue,
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsInt();

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = Strings.Chr(code);
        }
    }
}
