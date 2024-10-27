using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.BuiltInFields
{
    internal class ExecutionTimeParser : BaseParser
    {
        public static Regex ExecutionTimeRegex = new Regex("(\\bGlobals!ExecutionTime\\b)", RegexOptions.IgnoreCase);

        public ExecutionTimeParser(
            string currentString, 
            ExpressionFieldOperator op, 
            ReportExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, null, ExecutionTimeRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = ExecutionTimeRegex.Match(this.CurrentString);
            
            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(DateTime);
            this.CurrentExpression.Value = DateTime.Now;
        }
    }
}
