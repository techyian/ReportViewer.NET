﻿using ReportViewer.NET.DataObjects.ReportItems;
using ReportViewer.NET.Parsers.BuiltInFields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportViewer.NET.Parsers
{
    internal class ExpressionParser
    {
        private readonly TablixOperator[] ArithmeticOperators = { TablixOperator.Add, TablixOperator.Subtract, TablixOperator.Multiply, TablixOperator.Divide };
        private readonly TablixOperator[] ComparisonOperators = {
            TablixOperator.LessThan, TablixOperator.LessThanEqualTo, TablixOperator.GreaterThan, TablixOperator.GreaterThanEqualTo, TablixOperator.Equals,
            TablixOperator.NotEqual, TablixOperator.Like, TablixOperator.Is
        };
        private readonly TablixOperator[] LogicalOperators =
        {
            TablixOperator.And, TablixOperator.Not, TablixOperator.Or, TablixOperator.Xor,
            TablixOperator.AndAlso, TablixOperator.OrElse
        };

        public dynamic ParseTablixExpressionString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            string requestedFormat
        )
        {
            var expressions = RetrieveExpressionsFromString(tablixText, dataSetResults, values, dataSets);

            return ParseTablixExpressions(expressions, requestedFormat).Value;
        }

        public List<TablixExpression> RetrieveExpressionsFromString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets
        )
        {
            string currentString = tablixText.TrimStart('=');
            List<TablixExpression> expressions = new List<TablixExpression>();

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new TablixExpression();
                var proposedString = string.Empty;

                this.SearchBuiltInFields(currentString, currentExpression, ref proposedString);
                this.SearchAggregateFunctions(currentString, currentExpression, dataSetResults, values, dataSets, ref proposedString);
                this.SearchArithmeticOperators(currentString, currentExpression, ref proposedString);
                this.SearchComparisonOperators(currentString, currentExpression, ref proposedString);
                this.SearchProgramFlowFunctions(currentString, currentExpression, dataSetResults, values, dataSets, ref proposedString);
                this.SearchInspectionFunctions(currentString, currentExpression, dataSetResults, values, dataSets, ref proposedString);
                this.SearchLogicalOperators(currentString, currentExpression, ref proposedString);

                if (FieldParser.FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FieldParser.FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var fieldParser = new FieldParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, dataSets);
                    fieldParser.Parse();
                    proposedString = fieldParser.GetProposedString();
                }

                if ((currentExpression.Operator != TablixOperator.None && currentExpression.Index > 0) || currentExpression.Operator == TablixOperator.None)
                {
                    // We've resolved an expression but it isn't at the beginning of our string. Try and extract textual/numeric content at the start.
                    if (currentString.TrimStart().StartsWith('"'))
                    {
                        // Our string starts with a string literal. Extract this to the closing quote;
                        var extractedStringLiteral = ExtractStringLiteral(currentString);

                        currentExpression.ResolvedType = typeof(string);
                        currentExpression.Operator = TablixOperator.None;
                        currentExpression.Value = extractedStringLiteral.TrimStart('"').TrimEnd('"');
                        currentExpression.Index = currentString.IndexOf(extractedStringLiteral);
                        proposedString = currentString.Substring(currentExpression.Index + extractedStringLiteral.Length, currentString.Length - currentExpression.Index - extractedStringLiteral.Length);

                        // Clear up any whitespace not removed by statement above.
                        proposedString = proposedString.TrimStart();
                    }
                    else if (int.TryParse(currentString, out var parsedInt))
                    {
                        // We found an integer value, we can parse this and get out of the loop.
                        currentExpression.ResolvedType = typeof(int);
                        currentExpression.Operator = TablixOperator.None;
                        currentExpression.Value = parsedInt;
                        expressions.Add(currentExpression);

                        break;
                    }
                    else
                    {
                        // Take char by char and see if we can extract anything useful. If not, dump to a string and get out of loop.
                        var split = currentString.TrimStart().Split(' ');
                        var found = false;

                        for (var i = 0; i < split.Length; i++)
                        {
                            // Add in other possibilities? What about boolean expression?
                            if (int.TryParse(split[i], out var parsedSplitInt))
                            {
                                currentExpression.ResolvedType = typeof(int);
                                currentExpression.Operator = TablixOperator.None;
                                currentExpression.Value = parsedSplitInt;
                                currentExpression.Index = currentString.IndexOf(split[i]);
                                proposedString = currentString.Substring(currentExpression.Index + parsedSplitInt.ToString().Length, currentString.Length - currentExpression.Index - parsedSplitInt.ToString().Length);

                                // Clear up any whitespace not removed by statement above.
                                proposedString = proposedString.TrimStart();

                                found = true;

                                break;
                            }
                        }

                        if (!found)
                        {
                            currentExpression.ResolvedType = typeof(string);
                            currentExpression.Operator = TablixOperator.None;
                            currentExpression.Value = currentString.TrimStart('"').TrimEnd('"');
                            expressions.Add(currentExpression);

                            break;
                        }                        
                    }
                }

                expressions.Add(currentExpression);
                currentString = proposedString;
            }

            return expressions;
        }

        private void SearchBuiltInFields(
            string currentString,
            TablixExpression currentExpression,
            ref string proposedString
            )
        {
            if (ExecutionTimeParser.ExecutionTimeRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || ExecutionTimeParser.ExecutionTimeRegex.Match(currentString).Index < currentExpression.Index))
            {
                var executionTimeParser = new ExecutionTimeParser(currentString, TablixOperator.ExecutionTime, currentExpression, null, null, null);
                executionTimeParser.Parse();
                proposedString = executionTimeParser.GetProposedString();
            }
        }

        private void SearchArithmeticOperators(
            string currentString,
            TablixExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOf("+") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("+")) &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("+") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf("+");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Add;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("-") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("-")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("-") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("-");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Subtract;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("*") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("*")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("*") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("*");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Multiply;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("/") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("/")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("/") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("/");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Divide;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }
        }

        private void SearchComparisonOperators(
            string currentString,
            TablixExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOf(">=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf(">=")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf(">=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf(">=");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.GreaterThanEqualTo;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOf("<=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<=")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("<=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("<=");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.LessThanEqualTo;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOf(">") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf(">")) &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf(">") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf(">");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.GreaterThan;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("<") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<")) &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("<") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf("<");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.LessThan;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("=")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("=");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Equals;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("&") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("&")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("&") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("&");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.ConcatAnd;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("<>") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<>")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("<>") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("<>");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.NotEqual;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOf("Like") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("Like")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("Like") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("Like");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Like;

                proposedString = currentString.Substring(idx + 4, currentString.Length - idx - 4).TrimStart();
            }

            if (currentString.IndexOf("Is") > -1 && 
                !WithinStringLiteral(currentString, currentString.IndexOf("Is")) &&                 
                ((currentString.IndexOf("IsNothing") > -1 && currentString.IndexOf("Is") < currentString.IndexOf("IsNothing")) || currentString.IndexOf("IsNothing") == -1) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("Is") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("Is");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Is;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }
        }

        private void SearchConcatenationOperators(
            string currentString,
            TablixExpression currentExpression,
            ref string proposedString
        )
        {            
        }

        private void SearchLogicalOperators(
            string currentString,
            TablixExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOf("And") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("And")) &&
                ((currentString.IndexOf("AndNot") > -1 && currentString.IndexOf("And") < currentString.IndexOf("AndNot")) || currentString.IndexOf("AndNot") == -1) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("And") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("And");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.And;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }
        }

        private void SearchBitshiftOperators()
        {

        }

        private void SearchTextFunctions()
        {

        }

        private void SearchDateTimeFunctions()
        {

        }

        private void SearchMathFunctions()
        {

        }

        private void SearchInspectionFunctions(string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            ref string proposedString
        )
        {
            // IsArray
            // IsDate
            // IsNothing
            // IsNumeric

            if (IsNothingParser.IsNothingRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || IsNothingParser.IsNothingRegex.Match(currentString).Index < currentExpression.Index))
            {
                var isNothingParser = new IsNothingParser(currentString, TablixOperator.IsNothing, currentExpression, dataSetResults, values, dataSets);
                isNothingParser.Parse();
                proposedString = isNothingParser.GetProposedString();
            }
        }

        private void SearchProgramFlowFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            ref string proposedString
        )
        {
            // IIF
            // Choose
            // Switch

            if (IfParser.IfRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || IfParser.IfRegex.Match(currentString).Index < currentExpression.Index))
            {
                var ifParser = new IfParser(currentString, TablixOperator.If, currentExpression, dataSetResults, values, dataSets);
                ifParser.Parse();
                proposedString = ifParser.GetProposedString();
            }
        }

        private void SearchAggregateFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataObjects.DataSet> dataSets,
            ref string proposedString
        )
        {
            if (CountParser.CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || CountParser.CountRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var countParser = new CountParser(currentString, TablixOperator.Count, currentExpression, dataSetResults, values, dataSets);
                countParser.Parse();
                proposedString = countParser.GetProposedString();
            }

            if (FirstParser.FirstRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FirstParser.FirstRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var firstParser = new FirstParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, dataSets);
                firstParser.Parse();
                proposedString = firstParser.GetProposedString();
            }
        }

        private void SearchFinancialFunctions()
        {

        }

        private void SearchConversionFunctions()
        {

        }

        private void SearchMiscFunctions()
        {

        }

        private TablixExpression ParseTablixExpressions(IEnumerable<TablixExpression> expressions, string requestedFormat)
        {
            // Last logical operator to be checked on boolean returning operator/function.
            TablixOperator lastLogicalOperator = TablixOperator.None;
            bool lastLogicalOperatorValue = false;
                        
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
                    }
                    
                    switch (next.Operator)
                    {
                        case TablixOperator.Count:
                            newExpr.Value = double.Parse(prev.Value?.ToString() ?? "");
                            break;
                    }

                    this.ArithmeticOperatorAggregator(prev, next, newExpr);
                    this.ComparisonOperatorAggregator(prev, next, newExpr, lastLogicalOperator, ref lastLogicalOperatorValue);
                    this.LogicalOperatorAggregator(prev, next, newExpr, lastLogicalOperatorValue, ref lastLogicalOperator);

                    if (newExpr.Value == null)
                    {
                        newExpr.ResolvedType = next.ResolvedType;
                        newExpr.Value = next.Value;
                    }

                    return newExpr;
                });

                return EvaluateRequestedFormat(final, requestedFormat);
            }

            return null;
        }

        private void ArithmeticOperatorAggregator(TablixExpression prev, TablixExpression next, TablixExpression newExpr)
        {
            if (ArithmeticOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = prev.Value;
            }

            if (ComparisonOperators.Contains(prev.Operator))
            {
                newExpr.ResolvedType = typeof(double);
            }

            switch (prev.Operator)
            {
                case TablixOperator.Add:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") + double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.Subtract:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") - double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.Multiply:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") * double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.Divide:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") / double.Parse(next.Value?.ToString() ?? "");
                    break;
            }
        }

        private void ComparisonOperatorAggregator(TablixExpression prev, TablixExpression next, TablixExpression newExpr, TablixOperator lastLogicalOperator, ref bool booleanOperator)
        {
            if (ComparisonOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = prev.Value;
            }
                        
            switch (prev.Operator)
            {
                case TablixOperator.LessThan:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") < double.Parse(next.Value?.ToString() ?? "");                    
                    break;
                case TablixOperator.LessThanEqualTo:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") <= double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.GreaterThan:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") > double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.GreaterThanEqualTo:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") >= double.Parse(next.Value?.ToString() ?? "");
                    break;
                case TablixOperator.Equals:
                    switch (Type.GetTypeCode(prev.ResolvedType))
                    {
                        case TypeCode.DateTime:
                            newExpr.Value = prev.Value != null && next.Value != null ? DateTime.Parse(prev.Value.ToString()) == DateTime.Parse(next.Value.ToString()) : false;
                            break;
                        case TypeCode.String:
                            newExpr.Value = prev.Value != null && next.Value != null ? string.Compare(prev.Value.ToString(), next.Value.ToString()) == 0 : false;
                            break;
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            newExpr.Value = prev.Value != null && next.Value != null ? double.Parse(prev.Value.ToString()) == double.Parse(next.Value.ToString()) : false;
                            break;
                        case TypeCode.Object:
                            newExpr.Value = prev.Value == next.Value;
                            break;
                    }

                    break;
                case TablixOperator.NotEqual:
                    switch (Type.GetTypeCode(prev.ResolvedType))
                    {
                        case TypeCode.DateTime:
                            newExpr.Value = prev.Value != null && next.Value != null ? DateTime.Parse(prev.Value.ToString()) != DateTime.Parse(next.Value.ToString()) : false;
                            break;
                        case TypeCode.String:
                            newExpr.Value = prev.Value != null && next.Value != null ? string.Compare(prev.Value.ToString(), next.Value.ToString()) != 0 : false;
                            break;
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            newExpr.Value = prev.Value != null && next.Value != null ? double.Parse(prev.Value.ToString()) != double.Parse(next.Value.ToString()) : false;
                            break;
                        case TypeCode.Object:
                            newExpr.Value = prev.Value != next.Value;
                            break;
                    }

                    break;
            }

            if (ComparisonOperators.Contains(prev.Operator))
            {
                newExpr.ResolvedType = typeof(bool);
                booleanOperator = this.EvaluateLogicalOperator(lastLogicalOperator, (bool)newExpr.Value, booleanOperator);
            }
        }

        private void LogicalOperatorAggregator(TablixExpression prev, TablixExpression next, TablixExpression newExpr, bool booleanOperator, ref TablixOperator lastLogicalOperator)
        {
            if (LogicalOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = booleanOperator;
                lastLogicalOperator = next.Operator;
            }

            if (LogicalOperators.Contains(prev.Operator))
            {
                newExpr.ResolvedType = typeof(bool);
            }
        }

        private bool EvaluateLogicalOperator(TablixOperator lastLogicalOperator, bool valueA, bool valueB)
        {
            switch (lastLogicalOperator)
            {
                case TablixOperator.And:
                case TablixOperator.AndAlso:
                    return valueA && valueB;                    
                case TablixOperator.Not:
                    return !valueA;                    
                case TablixOperator.Or:
                case TablixOperator.OrElse:
                    return valueA || valueB;                    
                case TablixOperator.Xor:
                    return valueA ^ valueB;                
            }

            return valueA;
        }

        private TablixExpression EvaluateRequestedFormat(TablixExpression finalExpression, string requestedFormat)
        {
            if (string.IsNullOrEmpty(requestedFormat))
                return finalExpression;

            switch (Type.GetTypeCode(finalExpression.ResolvedType))
            {
                case TypeCode.DateTime:
                    finalExpression.Value = ((DateTime)finalExpression.Value).ToString(requestedFormat);
                    return finalExpression;
            }

            return finalExpression;
        }

        public static bool WithinStringLiteral(string currentString, int idx)
        {
            var inQuotes = false;

            for (var i = 0; i < currentString.Length; i++)
            {
                var value = currentString[i];

                if (i == idx && !inQuotes)
                {
                    return false;
                }

                if (value == '"' && !inQuotes)
                {
                    inQuotes = true;
                }
                else if (value == '"' && inQuotes)
                {
                    inQuotes = false;
                }
            }

            return true;
        }

        public static string ExtractStringLiteral(string currentString)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < currentString.Length; i++)
            {                
                sb.Append(currentString[i]);

                if (i + 1 < currentString.Length && currentString[i + 1] == '"')
                {
                    sb.Append(currentString[i + 1]);
                    return sb.ToString();
                }
            }

            return sb.ToString();
        }
    }
}