using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class CountParser : BaseParser
    {
        public static Regex CountRegex = new Regex("(?:\\(*?)(?:Count?)(\\((.*?)\\)\\)*)");

        public CountParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataObjects.DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, CountRegex)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (this.DataSetResults != null)
            {
                return (typeof(int), this.DataSetResults.Count());
            }
            else
            {
                var dataSet = this.DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null && dataSet.DataSetResults != null)
                {
                    return (typeof(int), dataSet.DataSetResults.Count());
                }
            }

            return (typeof(object), null);
        }

        public override void Parse()
        {            
            // TODO: Handle other count expressions not using fields??
            if (FieldParser.FieldRegex.IsMatch(this.CurrentString))
            {
                var match = FieldParser.FieldRegex.Match(this.CurrentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf('.', fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

                this.CurrentExpression.Index = match.Index;
                this.CurrentExpression.Field = fieldName;

                var dataSetStart = matchString.IndexOf('"', fieldEnd);

                if (dataSetStart > -1)
                {
                    var dataSetEnd = matchString.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                    var dataSetName = matchString.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);
                    this.CurrentExpression.DataSetName = dataSetName;
                }
                (Type, object) extractedValue = ExtractExpressionValue(fieldName, this.CurrentExpression.DataSetName);

                this.CurrentExpression.ResolvedType = extractedValue.Item1;
                this.CurrentExpression.Value = extractedValue.Item2;
            }
        }
    }
}
