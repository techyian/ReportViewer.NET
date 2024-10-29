using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class TodayParser : BaseParser
    {
        public static Regex TodayRegex = RegexCommon.GenerateMultiParamParserRegex("Today");

        public TodayParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, TodayRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = TodayRegex.Match(this.CurrentString);

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(DateTime);
            this.CurrentExpression.Value = DateTime.Now.Date;
        }
    }
}
