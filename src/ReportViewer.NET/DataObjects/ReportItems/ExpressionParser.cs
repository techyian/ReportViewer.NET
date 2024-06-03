using ReportViewer.NET.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    internal class ExpressionParser
    {        
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

                if (CountParser.CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || CountParser.CountRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var countParser = new CountParser(currentString, TablixOperator.Count, currentExpression, dataSetResults, values, dataSets);
                    countParser.Parse();
                    proposedString = countParser.GetProposedString();
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

                if (FieldParser.FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FieldParser.FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var fieldParser = new FieldParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, dataSets);
                    fieldParser.Parse();
                    proposedString = fieldParser.GetProposedString();
                }

                if (FirstParser.FirstRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FirstParser.FirstRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var firstParser = new FirstParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, dataSets);
                    firstParser.Parse();
                    proposedString = firstParser.GetProposedString();
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

                    if (next.ResolvedType == typeof(string))
                    {
                        // Treat all further expressions as string concatenation.
                        newExpr.ResolvedType = typeof(string);
                        newExpr.Value = $"{prev.Value}{next.Value}";
                        return newExpr;
                    }

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

            return value?.ToString() ?? string.Empty;
        }
    }
}
