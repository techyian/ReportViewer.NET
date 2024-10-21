using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.BuiltInFields
{
    public class ReportNameParser : BaseParser
    {
        public static Regex ReportNameRegex = new Regex("(\\bGlobals!ReportName\\b)", RegexOptions.IgnoreCase);

        public ReportNameParser(
            string currentString, 
            ExpressionFieldOperator op, 
            ReportExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            int currentRowNumber, 
            IEnumerable<DataSet> dataSets,                         
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, null, ReportNameRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = ReportNameRegex.Match(this.CurrentString);

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = this.Report.Name;
        }
    }
}
