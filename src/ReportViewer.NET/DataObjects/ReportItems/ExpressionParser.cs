using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    internal class ExpressionParser
    {
        public static Regex CountRegex = new Regex("(?:\\(*?)(?:Count?)(\\((.*?)\\)\\)*)");
        public static Regex FieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

        public string ParseTablixExpressionString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets,
            string requestedFormat
        )
        {
            string currentString = tablixText;
            List<TablixExpression> expressions = new List<TablixExpression>();

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new TablixExpression();
                var proposedString = string.Empty;

                if (CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || CountRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var regMatch = CountRegex.Match(currentString);
                    var group = regMatch.Groups[0];
                    var idx = group.Index;
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Count;

                    var endIndx = idx + group.Value.Length;

                    ParseCountExpression(currentString, currentExpression, dataSetResults, values, dataSets);

                    proposedString = endIndx == currentString.Length ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
                }

                if (currentString.IndexOf("+") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("+") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("+");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Add;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("-") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("-") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("-");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Subtract;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("*") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("*") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("*");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Multiply;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("/") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("/") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("/");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Divide;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var regMatch = FieldRegex.Match(currentString);
                    var group = regMatch.Groups[0];
                    var idx = group.Index;

                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Field;

                    var endIndx = idx + group.Value.Length;

                    ParseFieldExpression(currentString, currentExpression, dataSetResults, values, dataSets);

                    proposedString = endIndx == currentString.Length ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
                }

                if (currentExpression.Operator == TablixOperator.None)
                {
                    break;
                }

                expressions.Add(currentExpression);
                currentString = proposedString;
            }

            return ParseTablixExpression(expressions, requestedFormat);
        }

        public string ParseTablixExpression(IEnumerable<TablixExpression> expressions, string requestedFormat)
        {
            TablixOperator lastOperator;

            if (expressions.Count() > 0)
            {
                var final = expressions.Aggregate((prev, next) =>
                {
                    var newExpr = new TablixExpression();

                    newExpr.ResolvedType = prev.ResolvedType;

                    switch (next.Operator)
                    {
                        case TablixOperator.Count:
                            newExpr.Value = (int)prev.Value;
                            break;
                        case TablixOperator.Add:
                            newExpr.Value = (int)prev.Value + (int)next.Value;
                            break;
                        case TablixOperator.Subtract:
                            newExpr.Value = (int)prev.Value - (int)next.Value;
                            break;
                        case TablixOperator.Multiply:
                            newExpr.Value = (int)prev.Value * (int)next.Value;
                            break;
                        case TablixOperator.Divide:
                            newExpr.Value = (int)prev.Value / (int)next.Value;
                            break;
                    }

                    return newExpr;
                });

                return EvaluateRequestedFormat(final.Value, final.ResolvedType, requestedFormat);
            }

            return null;
        }

        public string EvaluateRequestedFormat(object value, Type resolvedType, string requestedFormat)
        {
            if (string.IsNullOrEmpty(requestedFormat))
                return value?.ToString() ?? string.Empty;

            switch (Type.GetTypeCode(resolvedType))
            {
                case TypeCode.DateTime:
                    return ((DateTime)value).ToString(requestedFormat);
            }

            return value.ToString() ?? string.Empty;
        }

        public void ParseCountExpression(
            string currentString,
            TablixExpression expression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets
        )
        {
            var fieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

            // TODO: Handle other count expressions not using fields??
            if (fieldRegex.IsMatch(currentString))
            {
                var match = fieldRegex.Match(currentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf('.', fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

                expression.Index = match.Index;
                expression.Field = fieldName;

                var dataSetStart = matchString.IndexOf('"', fieldEnd);

                if (dataSetStart > -1)
                {
                    var dataSetEnd = matchString.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                    var dataSetName = matchString.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);

                    expression.DataSetName = dataSetName;
                }

                (Type, object) extractedValue = ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetResults, values, dataSets);

                expression.ResolvedType = extractedValue.Item1;
                expression.Value = extractedValue.Item2;
            }
        }

        public void ParseFieldExpression(
            string currentString,
            TablixExpression expression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets
        )
        {
            var fieldsIdx = currentString.IndexOf("Fields!");
            var fieldEnd = currentString.IndexOf(".", fieldsIdx);
            var fieldName = currentString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

            expression.Index = fieldsIdx;
            expression.Field = fieldName;

            (Type, object) extractedValue = ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetResults, values, dataSets);

            expression.ResolvedType = extractedValue.Item1;
            expression.Value = extractedValue.Item2;
        }

        public (Type, object) ExtractExpressionValue(
            string dataSetName,
            string fieldName,
            TablixOperator op,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets
        )
        {
            if (dataSetResults != null)
            {
                switch (op)
                {
                    case TablixOperator.Count:
                        return (typeof(int), dataSetResults.Count());
                    case TablixOperator.Field:                       
                        if (values.ContainsKey(fieldName))
                        {
                            if (values[fieldName] == null)
                            {
                                return (typeof(object), null);
                            }

                            return (values[fieldName].GetType(), values[fieldName]);
                        }
                        break;
                }
            }
            else
            {
                var dataSet = dataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null)
                {
                    if (dataSet.DataSetResults != null)
                    {
                        switch (op)
                        {
                            case TablixOperator.Count:
                                return (typeof(int), dataSet.DataSetResults.Count());
                            case TablixOperator.Field:
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
                                break;
                        }
                    }
                }
            }

            return (typeof(object), null);
        }
    }
}
