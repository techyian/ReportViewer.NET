using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.BuiltInFields
{
    internal class ExecutionTimeParser : BaseParser
    {
        public static Regex ExecutionTimeRegex = new Regex("(\\bGlobals!ExecutionTime\\b)");

        public ExecutionTimeParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, ExecutionTimeRegex)
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
            this.CurrentExpression.ResolvedType = typeof(string);
            this.CurrentExpression.Value = DateTime.Now.ToString();
        }
    }
}
