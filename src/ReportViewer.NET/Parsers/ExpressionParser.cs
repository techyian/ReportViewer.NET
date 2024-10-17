using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers.Aggregate;
using ReportViewer.NET.Parsers.BuiltInFields;
using ReportViewer.NET.Parsers.DateAndTime;
using ReportViewer.NET.Parsers.Inspection;
using ReportViewer.NET.Parsers.Misc;
using ReportViewer.NET.Parsers.ProgramFlow;
using ReportViewer.NET.Parsers.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportViewer.NET.Parsers
{
    public class ExpressionParser
    {
        private readonly TablixOperator[] ArithmeticOperators = { TablixOperator.Add, TablixOperator.Subtract, TablixOperator.Multiply, TablixOperator.Divide, TablixOperator.Mod };

        private readonly TablixOperator[] ComparisonOperators = {
            TablixOperator.LessThan, TablixOperator.LessThanEqualTo, TablixOperator.GreaterThan, TablixOperator.GreaterThanEqualTo, TablixOperator.Equals,
            TablixOperator.NotEqual, TablixOperator.Like, TablixOperator.Is
        };

        private readonly TablixOperator[] LogicalOperators =
        {
            TablixOperator.And, TablixOperator.Not, TablixOperator.Or, TablixOperator.Xor,
            TablixOperator.AndAlso, TablixOperator.OrElse
        };

        private readonly TablixOperator[] ConcatenationOperators =
        {
            TablixOperator.ConcatAnd, TablixOperator.ConcatPlus
        };

        private readonly TablixOperator[] AggregateOperators =
        {
            TablixOperator.Count, TablixOperator.Sum
        };

        private ReportRDL _report;

        public ExpressionParser(ReportRDL report)
        {
            _report = report;
        }

        public object ParseTablixExpressionString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            string requestedFormat
        )
        {
            if (string.IsNullOrEmpty(tablixText))
            {
                return null;
            }

            var expressions = RetrieveExpressionsFromString(tablixText, dataSetResults, values, currentRowNumber, dataSets, activeDataset);

            return ParseTablixExpressions(expressions, requestedFormat).Value;
        }

        public List<TablixExpression> RetrieveExpressionsFromString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset
        )
        {
            string currentString = tablixText.TrimStart('=').TrimStart();
            List<TablixExpression> expressions = new List<TablixExpression>();

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new TablixExpression();
                var proposedString = string.Empty;

                this.SearchBuiltInFields(currentString, currentExpression, ref proposedString);
                this.SearchAggregateFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchArithmeticOperators(currentString, currentExpression, ref proposedString);
                this.SearchComparisonOperators(currentString, currentExpression, ref proposedString);
                this.SearchProgramFlowFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchInspectionFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchLogicalOperators(currentString, currentExpression, ref proposedString);
                this.SearchTextFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchDateTimeFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchMiscFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);

                if (FieldParser.FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FieldParser.FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var fieldParser = new FieldParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                    fieldParser.Parse();
                    proposedString = fieldParser.GetProposedString();
                }

                if (ParameterParser.ParameterRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || ParameterParser.ParameterRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var paramParser = new ParameterParser(currentString, TablixOperator.Parameter, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                    paramParser.Parse();
                    proposedString = paramParser.GetProposedString();
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
                var executionTimeParser = new ExecutionTimeParser(currentString, TablixOperator.ExecutionTime, currentExpression, null, null, 0, null, _report);
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

            if (currentString.IndexOfIgnore("Mod") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Mod")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOfIgnore("Mod") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Mod");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Mod;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
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

            if (currentString.IndexOfIgnore("Like") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Like")) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOfIgnore("Like") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Like");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Like;

                proposedString = currentString.Substring(idx + 4, currentString.Length - idx - 4).TrimStart();
            }

            if (currentString.IndexOfIgnore("Is") > -1 &&
                !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Is")) &&
                ((currentString.IndexOfIgnore("IsNothing") > -1 && currentString.IndexOfIgnore("Is") < currentString.IndexOfIgnore("IsNothing")) || currentString.IndexOfIgnore("IsNothing") == -1) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOfIgnore("Is") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Is");
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
            if (currentString.IndexOfIgnore("And") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("And")) &&
                ((currentString.IndexOfIgnore("AndNot") > -1 && currentString.IndexOfIgnore("And") < currentString.IndexOfIgnore("AndNot")) || currentString.IndexOfIgnore("AndNot") == -1) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOfIgnore("And") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("And");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.And;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }

            if (currentString.IndexOfIgnore("Not") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Not")) &&
                ((currentString.IndexOfIgnore("AndNot") > -1 && currentString.IndexOfIgnore("Not") < currentString.IndexOfIgnore("AndNot")) || currentString.IndexOfIgnore("AndNot") == -1) &&
                (currentExpression.Operator == TablixOperator.None || currentString.IndexOfIgnore("Not") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Not");
                currentExpression.Index = idx;
                currentExpression.Operator = TablixOperator.Not;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }
        }

        private void SearchBitshiftOperators()
        {

        }

        private void SearchTextFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (LeftParser.LeftRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || LeftParser.LeftRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var leftParser = new LeftParser(currentString, TablixOperator.Left, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                leftParser.Parse();
                proposedString = leftParser.GetProposedString();
            }

            if (FormatCurrencyParser.FormatCurrencyRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || FormatCurrencyParser.FormatCurrencyRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var fcParser = new FormatCurrencyParser(currentString, TablixOperator.FormatCurrency, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                fcParser.Parse();
                proposedString = fcParser.GetProposedString();
            }
        }

        private void SearchDateTimeFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (MonthNameParser.MonthNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || MonthNameParser.MonthNameRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var mnParser = new MonthNameParser(currentString, TablixOperator.MonthName, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                mnParser.Parse();
                proposedString = mnParser.GetProposedString();
            }
        }

        private void SearchMathFunctions()
        {

        }

        private void SearchInspectionFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
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
                var isNothingParser = new IsNothingParser(currentString, TablixOperator.IsNothing, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                isNothingParser.Parse();
                proposedString = isNothingParser.GetProposedString();
            }
        }

        private void SearchProgramFlowFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            // IIF
            // Choose
            // Switch

            if (IfParser.IfRegex.IsMatch(currentString) &&
                (currentExpression.Operator == TablixOperator.None || IfParser.IfRegex.Match(currentString).Index < currentExpression.Index))
            {
                var ifParser = new IfParser(currentString, TablixOperator.If, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                ifParser.Parse();
                proposedString = ifParser.GetProposedString();
            }
        }

        private void SearchAggregateFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (CountParser.CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || CountParser.CountRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var countParser = new CountParser(currentString, TablixOperator.Count, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                countParser.Parse();
                proposedString = countParser.GetProposedString();
            }

            if (CountDistinctParser.CountDistinctRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || CountDistinctParser.CountDistinctRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var countDistinctParser = new CountDistinctParser(currentString, TablixOperator.CountDistinct, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                countDistinctParser.Parse();
                proposedString = countDistinctParser.GetProposedString();
            }

            if (SumParser.SumRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || SumParser.SumRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var sumParser = new SumParser(currentString, TablixOperator.Sum, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                sumParser.Parse();
                proposedString = sumParser.GetProposedString();
            }

            if (FirstParser.FirstRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FirstParser.FirstRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var firstParser = new FirstParser(currentString, TablixOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
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

        private void SearchMiscFunctions(
            string currentString,
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (RowNumberParser.RowNumberRegex.IsMatch(currentString) &&
               (currentExpression.Operator == TablixOperator.None || RowNumberParser.RowNumberRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var rnParser = new RowNumberParser(currentString, TablixOperator.RowNumber, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                rnParser.Parse();
                proposedString = rnParser.GetProposedString();
            }
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

                    if (AggregateOperators.Contains(next.Operator))
                    {
                        newExpr.Value = double.Parse(prev.Value?.ToString() ?? "");
                    }

                    this.ArithmeticOperatorAggregator(prev, next, newExpr);
                    this.ComparisonOperatorAggregator(prev, next, newExpr, lastLogicalOperator, ref lastLogicalOperatorValue);
                    this.LogicalOperatorAggregator(prev, next, newExpr, lastLogicalOperatorValue, ref lastLogicalOperator);
                    this.ConcatenationOperatorAggregator(prev, next, newExpr);

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
                case TablixOperator.Mod:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") % double.Parse(next.Value?.ToString() ?? "");
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

                if (prev.Operator == TablixOperator.Not && next.ResolvedType == typeof(bool))
                {
                    // Negate next value
                    newExpr.Value = !(bool)next.Value;
                }
            }
        }

        private void ConcatenationOperatorAggregator(TablixExpression prev, TablixExpression next, TablixExpression newExpr)
        {
            if (ConcatenationOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = prev.Value;
            }

            switch (prev.Operator)
            {
                case TablixOperator.ConcatAnd:
                    newExpr.Value = prev.Value.ToString() + next.Value.ToString();
                    break;
                case TablixOperator.ConcatPlus:
                    if (int.TryParse(prev.Value.ToString(), out var prevInt) && int.TryParse(next.Value.ToString(), out var nextInt))
                    {
                        newExpr.Value = prevInt + nextInt;
                    }
                    else
                    {
                        newExpr.Value = prev.Value.ToString() + next.Value.ToString();
                    }

                    break;
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
                case TypeCode.Int32:
                    finalExpression.Value = int.Parse(finalExpression.Value.ToString()).ToString(requestedFormat);
                    return finalExpression;
                case TypeCode.Double:
                    finalExpression.Value = double.Parse(finalExpression.Value.ToString()).ToString(requestedFormat);
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

        public static bool ContainsAggregatorExpression(string value)
        {
            return CountParser.CountRegex.IsMatch(value) || SumParser.SumRegex.IsMatch(value);
        }

        public static bool ContainsRepeatExpression(string value)
        {
            return !ContainsAggregatorExpression(value) && FieldParser.FieldRegex.IsMatch(value);
        }
    }
}
