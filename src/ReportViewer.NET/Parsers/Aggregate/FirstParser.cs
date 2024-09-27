using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.Aggregate
{
    public class FirstParser : BaseParser
    {
        public static Regex FirstRegex = RegexCommon.GenerateParserRegex("First");

        public FirstParser(
            string currentString,
            TablixOperator op,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, FirstRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (DataSetResults != null)
            {
                var first = DataSetResults.FirstOrDefault();

                if (first != null && first.ContainsKey(fieldName))
                {
                    if (first[fieldName] == null)
                    {
                        return (typeof(object), null);
                    }

                    return (first[fieldName].GetType(), first[fieldName]);
                }
            }
            else
            {
                var dataSet = DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null && dataSet.DataSetResults != null)
                {
                    var first = dataSet.DataSetResults.FirstOrDefault();

                    if (first != null && first.ContainsKey(fieldName))
                    {
                        if (first[fieldName] == null)
                        {
                            return (typeof(object), null);
                        }

                        return (first[fieldName].GetType(), first[fieldName]);
                    }
                }
            }

            return (typeof(object), null);
        }

        public override void Parse()
        {
            // TODO: Filtering on field value e.g. =First(Fields!MiddleInitial.Value = "P")
            // TODO: Filtering on field value by parameter value e.g. =First(Fields!MiddleInitial.Value = Parameters!MiddleInitial.Value(0))
            var match = FirstRegex.Match(CurrentString);
            var matchString = match.Value;

            var fieldsIdx = matchString.IndexOf("Fields!");
            var fieldEnd = matchString.IndexOf('.', fieldsIdx);
            var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

            CurrentExpression.Index = match.Index;
            CurrentExpression.Field = fieldName;

            var dataSetStart = matchString.IndexOf('"', fieldEnd);

            if (dataSetStart > -1)
            {
                var dataSetEnd = matchString.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                var dataSetName = matchString.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);
                CurrentExpression.DataSetName = dataSetName;
            }
            (Type, object) extractedValue = ExtractExpressionValue(fieldName, CurrentExpression.DataSetName);

            CurrentExpression.ResolvedType = extractedValue.Item1;
            CurrentExpression.Value = extractedValue.Item2;
        }
    }
}
