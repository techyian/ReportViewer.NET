using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using ReportViewer.NET.Extensions;
using System.Text;
using Microsoft.VisualBasic;

namespace ReportViewer.NET.Parsers.Text
{
    public class AscParser : BaseParser
    {
        public static Regex AscRegex = RegexCommon.GenerateParserRegex("Asc");

        public AscParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, AscRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = AscRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Asc including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(4);
                        
            var str = this.Report.Parser.ParseReportExpressionString(
                matchValue,
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsString();

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(int);
            this.CurrentExpression.Value = Strings.Asc(str);
        }
    }
}
