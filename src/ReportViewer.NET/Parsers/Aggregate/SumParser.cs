using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Aggregate
{
    public class SumParser : BaseParser
    {
        public static Regex SumRegex = new Regex("(?:\\(*?)(?:Sum?)(\\((.*?)\\)\\)*)", RegexOptions.IgnoreCase);

        public SumParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            DataObjects.DataSet activeDataset
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, SumRegex)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            fieldName = fieldName.ToLower();

            if (DataSetResults != null)
            {
                var results = DataSetResults.Where(dsr => dsr.ContainsKey(fieldName)).Select(dsr => dsr[fieldName]);
                double total = 0;

                foreach (var res in results)
                {
                    total += double.Parse(res.ToString());
                }

                return (typeof(double), total);
            }
            else
            {
                var dataSet = DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null && dataSet.DataSetResults != null)
                {
                    var results = dataSet.DataSetResults.Where(dsr => dsr.ContainsKey(fieldName)).Select(dsr => dsr[fieldName]);
                    double total = 0;

                    foreach (var res in results)
                    {
                        total += double.Parse(res.ToString());
                    }

                    return (typeof(double), total);
                }
            }

            return (typeof(double), 0);
        }

        public override void Parse()
        {
            // TODO: Handle other sum expressions not using fields??
            var sumMatch = SumRegex.Match(CurrentString);
            var sumValue = sumMatch.Value;

            if (FieldParser.FieldDatasetRegex.IsMatch(sumValue))
            {
                var fieldDataSetMatch = FieldParser.FieldDatasetRegex.Match(sumValue);
                var fieldDataSetValue = fieldDataSetMatch.Value;
                var dataSetStart = fieldDataSetValue.IndexOf('"');

                if (dataSetStart > -1)
                {
                    var dataSetEnd = fieldDataSetValue.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                    var dataSetName = fieldDataSetValue.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);
                    CurrentExpression.DataSetName = dataSetName;
                }
            }

            if (FieldParser.FieldRegex.IsMatch(sumValue))
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
