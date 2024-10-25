using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class DateStringParser : BaseParser
    {
        public static Regex DateStringRegex = RegexCommon.GenerateParserRegex("DateString");

        public DateStringParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, DateStringRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = DateStringRegex.Match(CurrentString);

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = typeof(DateTime);
            CurrentExpression.Value = DateTime.Now;
        }
    }
}
