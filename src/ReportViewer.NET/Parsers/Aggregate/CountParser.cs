using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Aggregate
{
    public class CountParser : BaseParser
    {
        public static Regex CountRegex = RegexCommon.GenerateParserRegex("Count");

        public CountParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            DataObjects.DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, CountRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (DataSetResults != null)
            {
                return (typeof(int), DataSetResults.Count());
            }
            else
            {
                var dataSet = DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

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
            var countMatch = CountRegex.Match(CurrentString);
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
                    CurrentExpression.DataSetName = dataSetName;
                }
            }

            if (FieldParser.FieldRegex.IsMatch(countValue))
            {
                var match = FieldParser.FieldRegex.Match(CurrentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf('.', fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

                CurrentExpression.Field = fieldName;

                (Type, object) extractedValue = ExtractExpressionValue(fieldName, CurrentExpression.DataSetName);

                CurrentExpression.ResolvedType = extractedValue.Item1;
                CurrentExpression.Value = extractedValue.Item2;
            }
        }
    }
}
