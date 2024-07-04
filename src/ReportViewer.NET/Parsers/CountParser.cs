using Microsoft.AspNetCore.Http;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class CountParser : BaseParser
    {
        public static Regex CountRegex = new Regex("(?:\\(*?)(?:Count?)(\\((.*?)\\)\\)*)", RegexOptions.IgnoreCase);

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
            var countMatch = CountRegex.Match(this.CurrentString);
            var countValue = countMatch.Value;

            if (FieldParser.FieldDatasetRegex.IsMatch(countValue))
            {
                var fieldDataSetMatch = FieldParser.FieldDatasetRegex.Match(countValue);
                var fieldDataSetValue = fieldDataSetMatch.Value;
                var dataSetStart = fieldDataSetValue.IndexOf('"');

                if (dataSetStart > -1)
                {
                    var dataSetEnd = fieldDataSetValue.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                    var dataSetName = fieldDataSetValue.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);
                    this.CurrentExpression.DataSetName = dataSetName;
                }                                
            }

            if (FieldParser.FieldRegex.IsMatch(countValue))
            {
                var match = FieldParser.FieldRegex.Match(this.CurrentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf('.', fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

                this.CurrentExpression.Field = fieldName;

                (Type, object) extractedValue = ExtractExpressionValue(fieldName, this.CurrentExpression.DataSetName);

                this.CurrentExpression.ResolvedType = extractedValue.Item1;
                this.CurrentExpression.Value = extractedValue.Item2;
            }
        }
    }
}
