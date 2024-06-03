using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class FieldParser : BaseParser
    {
        public static Regex FieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

        public FieldParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataObjects.DataSet> dataSets
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, FieldRegex)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            if (this.DataSetResults != null)
            {
                if (this.Values != null && this.Values.ContainsKey(fieldName))
                {
                    if (this.Values[fieldName] == null)
                    {
                        return (typeof(object), null);
                    }

                    return (this.Values[fieldName].GetType(), this.Values[fieldName]);
                }
            }
            else
            {
                var dataSet = this.DataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null && dataSet.DataSetResults != null)
                {
                    foreach (IDictionary<string, object> expando in dataSet.DataSetResults)
                    {
                        if (expando.ContainsKey(fieldName))
                        {
                            if (expando[fieldName] == null)
                            {
                                return (typeof(object), null);
                            }

                            return (expando[fieldName].GetType(), expando[fieldName]);
                        }
                    }
                }
            }

            return (typeof(object), null);
        }

        public override void Parse()
        {
            var fieldsIdx = this.CurrentString.IndexOf("Fields!");
            var fieldEnd = this.CurrentString.IndexOf(".", fieldsIdx);
            var fieldName = this.CurrentString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

            this.CurrentExpression.Index = fieldsIdx;
            this.CurrentExpression.Field = fieldName;
            (Type, object) extractedValue = ExtractExpressionValue(fieldName, this.CurrentExpression.DataSetName);

            this.CurrentExpression.ResolvedType = extractedValue.Item1;
            this.CurrentExpression.Value = extractedValue.Item2;
        }

        public string ExtractDataSetName()
        {
            var matchString = this.RegexMatch.Value;

            var fieldsIdx = matchString.IndexOf("Fields!");
            var fieldEnd = matchString.IndexOf('.', fieldsIdx);
            var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

            this.CurrentExpression.Index = this.RegexMatch.Index;
            this.CurrentExpression.Field = fieldName;

            var dataSetStart = matchString.IndexOf('"', fieldEnd);

            if (dataSetStart > -1)
            {
                var dataSetEnd = matchString.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                var dataSetName = matchString.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);
                return dataSetName;
            }

            return string.Empty;
        }
    }
}
