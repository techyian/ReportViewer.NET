using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class FirstParser : BaseParser
    {
        public static Regex FirstRegex = new Regex("(?:\\(*?)(?:First?)(\\((.*?)\\)\\)*)");

        public FirstParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, FirstRegex)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (this.DataSetResults != null)
            {
                var first = this.DataSetResults.FirstOrDefault();

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
                var dataSet = this.DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

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
            var fieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");
                        
            if (fieldRegex.IsMatch(this.CurrentString))
            {
                var match = fieldRegex.Match(this.CurrentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf('.', fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

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
