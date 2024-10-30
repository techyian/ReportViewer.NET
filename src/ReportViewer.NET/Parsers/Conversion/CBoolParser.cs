using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using ReportViewer.NET.Extensions;

namespace ReportViewer.NET.Parsers.Conversion
{    
    public class CBoolParser : BaseParser
    {
        public static Regex CBoolRegex = RegexCommon.GenerateMultiParamParserRegex("CBool");

        public CBoolParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, CBoolRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = CBoolRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding CBool including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
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
            this.CurrentExpression.ResolvedType = typeof(bool);
            this.CurrentExpression.Value = Convert.ToBoolean(expr);
        }
    }
}
