using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Misc
{
    internal class RowNumberParser : BaseParser
    {
        public static Regex RowNumberRegex = RegexCommon.GenerateParserRegex("RowNumber");

        public RowNumberParser(
            string currentString, 
            ExpressionFieldOperator op, 
            ReportExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            int currentRowNumber, 
            IEnumerable<DataSet> dataSets, 
            DataSet activeDataset, 
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, RowNumberRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (dataSetName.EqualsIgnore("Nothing") && this.ActiveDataset != null)
            {
                return (typeof(int), this.ActiveDataset.CurrentRowNumber);
            }

            return (typeof(int), this.CurrentRowNumber);
        }

        public override void Parse()
        {
            var match = RowNumberRegex.Match(this.CurrentString);            
            var matchValue = match.Value.Replace("\n", "").Replace("\t", "");                        
            matchValue = matchValue.Substring(9, matchValue.Length - 10);

            var value = this.ExtractExpressionValue(null, matchValue);

            CurrentExpression.Index = match.Index;
            CurrentExpression.ResolvedType = value.Item1;
            CurrentExpression.Value = value.Item2;
        }
    }
}
