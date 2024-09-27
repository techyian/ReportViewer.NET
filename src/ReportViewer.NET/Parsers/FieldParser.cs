using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public class FieldParser : BaseParser
    {
        public static Regex FieldRegex = new Regex("(\\bFields!\\b(.*?)\\.Value)", RegexOptions.IgnoreCase);
        public static Regex FieldDatasetRegex = new Regex("(\\bFields!\\b(.*?)\\))", RegexOptions.IgnoreCase);

        private readonly ExpressionParser _expressionParser;

        public FieldParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, dataSets, activeDataset, FieldRegex, report)
        {
            _expressionParser = new ExpressionParser(report);
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            fieldName = fieldName.ToLower();

            // Search for fieldname in datasets to see if it's a calculated field.
            if (this.ActiveDataset != null && this.ActiveDataset.Fields.Any(f => !string.IsNullOrEmpty(f.Name) && f.Name.ToLower() == fieldName && !string.IsNullOrEmpty(f.Value)))
            {
                var calcField = this.ActiveDataset.Fields.First(f => f.Name.ToLower() == fieldName && !string.IsNullOrEmpty(f.Value)).Value;
                var resolvedValue = _expressionParser.ParseTablixExpressionString(calcField, this.DataSetResults, this.Values, this.DataSets, this.ActiveDataset, null);

                return (resolvedValue.GetType(), resolvedValue);
            }
            else if (this.DataSets.Any(ds => ds.Fields != null && ds.Fields.Any(f => !string.IsNullOrEmpty(f.Name) && f.Name.ToLower() == fieldName && !string.IsNullOrEmpty(f.Value))))
            {
                var ds = this.DataSets.First(ds => ds.Fields.Any(f => f.Name.ToLower() == fieldName && !string.IsNullOrEmpty(f.Value)));
                var calcField = ds.Fields.First(f => f.Name.ToLower() == fieldName && !string.IsNullOrEmpty(f.Value)).Value;

                var resolvedValue = _expressionParser.ParseTablixExpressionString(calcField, this.DataSetResults, this.Values, this.DataSets, this.ActiveDataset, null);
                
                return (resolvedValue.GetType(), resolvedValue);
            }

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
                else
                {
                    // Get first result from DataSetResults and parse requested field.
                    var first = this.DataSetResults.First();

                    if (first.ContainsKey(fieldName))
                    {
                        if (first[fieldName] == null)
                        {
                            return (typeof(object), null);
                        }

                        return (first[fieldName].GetType(), first[fieldName]);
                    }                    
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
            var fieldName = this.CurrentString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

            this.CurrentExpression.Index = fieldsIdx;
            this.CurrentExpression.Field = fieldName;
            (Type, object) extractedValue = ExtractExpressionValue(fieldName, this.CurrentExpression.DataSetName);

            this.CurrentExpression.ResolvedType = extractedValue.Item1;
            this.CurrentExpression.Value = extractedValue.Item2;
        }

        public string ExtractDataSetName()
        {
            var match = FieldDatasetRegex.Match(this.CurrentString);
            var matchString = match.Value;

            if (string.IsNullOrEmpty(match.Value))
            {
                return string.Empty;
            }

            var fieldsIdx = matchString.IndexOf("Fields!");
            var fieldEnd = matchString.IndexOf('.', fieldsIdx);
            var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

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
