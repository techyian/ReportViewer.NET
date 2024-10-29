using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Conversion
{
    public class CIntParser : BaseParser
    {
        public static Regex CIntRegex = RegexCommon.GenerateMultiParamParserRegex("CInt");

        public CIntParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, CIntRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = CIntRegex.Match(CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding CInt including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(5);
                        
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
            this.CurrentExpression.ResolvedType = typeof(long);
            this.CurrentExpression.Value = Convert.ToInt64(expr, CultureInfo.InvariantCulture);
        }
    }
}
