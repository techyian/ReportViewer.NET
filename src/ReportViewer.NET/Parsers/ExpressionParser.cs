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
        private readonly ExpressionFieldOperator[] ArithmeticOperators = { ExpressionFieldOperator.Add, ExpressionFieldOperator.Subtract, ExpressionFieldOperator.Multiply, ExpressionFieldOperator.Divide, ExpressionFieldOperator.Mod };

        private readonly ExpressionFieldOperator[] ComparisonOperators = {
            ExpressionFieldOperator.LessThan, ExpressionFieldOperator.LessThanEqualTo, ExpressionFieldOperator.GreaterThan, ExpressionFieldOperator.GreaterThanEqualTo, ExpressionFieldOperator.Equals,
            ExpressionFieldOperator.NotEqual, ExpressionFieldOperator.Like, ExpressionFieldOperator.Is
        };

        private readonly ExpressionFieldOperator[] LogicalOperators =
        {
            ExpressionFieldOperator.And, ExpressionFieldOperator.Not, ExpressionFieldOperator.Or, ExpressionFieldOperator.Xor,
            ExpressionFieldOperator.AndAlso, ExpressionFieldOperator.OrElse
        };

        private readonly ExpressionFieldOperator[] ConcatenationOperators =
        {
            ExpressionFieldOperator.ConcatAnd, ExpressionFieldOperator.ConcatPlus
        };

        private readonly ExpressionFieldOperator[] AggregateOperators =
        {
            ExpressionFieldOperator.Count, ExpressionFieldOperator.Sum
        };

        private ReportRDL _report;

        public ExpressionParser(ReportRDL report)
        {
            _report = report;
        }

        public object ParseReportExpressionString(
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

            return ParseReportExpressions(expressions, requestedFormat).Value;
        }

        public List<ReportExpression> RetrieveExpressionsFromString(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset
        )
        {
            if (tablixText.StartsWith("="))
            {
                // As we're processing an expression, clear out newlines and tabs.
                tablixText = tablixText.Replace("\n", "").Replace("\t", "");
            }

            string currentString = tablixText.TrimStart('=').TrimStart();
            List<ReportExpression> expressions = new List<ReportExpression>();

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new ReportExpression();
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
                    (currentExpression.Operator == ExpressionFieldOperator.None || FieldParser.FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var fieldParser = new FieldParser(currentString, ExpressionFieldOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                    fieldParser.Parse();
                    proposedString = fieldParser.GetProposedString();
                }

                if (ParameterParser.ParameterRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || ParameterParser.ParameterRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var paramParser = new ParameterParser(currentString, ExpressionFieldOperator.Parameter, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                    paramParser.Parse();
                    proposedString = paramParser.GetProposedString();
                }

                if ((currentExpression.Operator != ExpressionFieldOperator.None && currentExpression.Index > 0) || currentExpression.Operator == ExpressionFieldOperator.None)
                {
                    // We've resolved an expression but it isn't at the beginning of our string. Try and extract textual/numeric content at the start.
                    if (currentString.TrimStart().StartsWith('"'))
                    {
                        // Our string starts with a string literal. Extract this to the closing quote;
                        var extractedStringLiteral = ExtractStringLiteral(currentString);

                        currentExpression.ResolvedType = typeof(string);
                        currentExpression.Operator = ExpressionFieldOperator.None;
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
                        currentExpression.Operator = ExpressionFieldOperator.None;
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
                                currentExpression.Operator = ExpressionFieldOperator.None;
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
                            currentExpression.Operator = ExpressionFieldOperator.None;
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
            ReportExpression currentExpression,
            ref string proposedString
            )
        {
            if (ExecutionTimeParser.ExecutionTimeRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ExecutionTimeParser.ExecutionTimeRegex.Match(currentString).Index < currentExpression.Index))
            {
                var executionTimeParser = new ExecutionTimeParser(currentString, ExpressionFieldOperator.ExecutionTime, currentExpression, null, null, 0, null, _report);
                executionTimeParser.Parse();
                proposedString = executionTimeParser.GetProposedString();
            }

            if (LanguageParser.LanguageRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || LanguageParser.LanguageRegex.Match(currentString).Index < currentExpression.Index))
            {
                var languageParser = new LanguageParser(currentString, ExpressionFieldOperator.Language, currentExpression, null, null, 0, null, _report);
                languageParser.Parse();
                proposedString = languageParser.GetProposedString();
            }

            if (ReportNameParser.ReportNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ReportNameParser.ReportNameRegex.Match(currentString).Index < currentExpression.Index))
            {
                var reportNameParser = new ReportNameParser(currentString, ExpressionFieldOperator.ReportName, currentExpression, null, null, 0, null, _report);
                reportNameParser.Parse();
                proposedString = reportNameParser.GetProposedString();
            }
        }

        private void SearchArithmeticOperators(
            string currentString,
            ReportExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOf("+") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("+")) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("+") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf("+");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Add;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("-") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("-")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("-") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("-");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Subtract;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("*") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("*")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("*") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("*");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Multiply;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("/") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("/")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("/") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("/");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Divide;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOfIgnore("Mod") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Mod")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOfIgnore("Mod") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Mod");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Mod;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }
        }

        private void SearchComparisonOperators(
            string currentString,
            ReportExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOf(">=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf(">=")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf(">=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf(">=");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.GreaterThanEqualTo;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOf("<=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<=")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("<=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("<=");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.LessThanEqualTo;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOf(">") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf(">")) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf(">") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf(">");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.GreaterThan;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("<") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<")) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("<") < currentExpression.Index)
                )
            {
                var idx = currentString.IndexOf("<");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.LessThan;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("=") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("=")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("=") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("=");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Equals;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("&") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("&")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("&") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("&");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.ConcatAnd;

                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
            }

            if (currentString.IndexOf("<>") > -1 && !WithinStringLiteral(currentString, currentString.IndexOf("<>")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOf("<>") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOf("<>");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.NotEqual;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }

            if (currentString.IndexOfIgnore("Like") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Like")) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOfIgnore("Like") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Like");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Like;

                proposedString = currentString.Substring(idx + 4, currentString.Length - idx - 4).TrimStart();
            }

            if (currentString.IndexOfIgnore("Is") > -1 &&
                !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Is")) &&
                ((currentString.IndexOfIgnore("IsNothing") > -1 && currentString.IndexOfIgnore("Is") < currentString.IndexOfIgnore("IsNothing")) || currentString.IndexOfIgnore("IsNothing") == -1) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOfIgnore("Is") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Is");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Is;

                proposedString = currentString.Substring(idx + 2, currentString.Length - idx - 2).TrimStart();
            }
        }

        private void SearchConcatenationOperators(
            string currentString,
            ReportExpression currentExpression,
            ref string proposedString
        )
        {
        }

        private void SearchLogicalOperators(
            string currentString,
            ReportExpression currentExpression,
            ref string proposedString
        )
        {
            if (currentString.IndexOfIgnore("And") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("And")) &&
                ((currentString.IndexOfIgnore("AndNot") > -1 && currentString.IndexOfIgnore("And") < currentString.IndexOfIgnore("AndNot")) || currentString.IndexOfIgnore("AndNot") == -1) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOfIgnore("And") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("And");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.And;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }

            if (currentString.IndexOfIgnore("Not") > -1 && !WithinStringLiteral(currentString, currentString.IndexOfIgnore("Not")) &&
                ((currentString.IndexOfIgnore("AndNot") > -1 && currentString.IndexOfIgnore("Not") < currentString.IndexOfIgnore("AndNot")) || currentString.IndexOfIgnore("AndNot") == -1) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || currentString.IndexOfIgnore("Not") < currentExpression.Index)
            )
            {
                var idx = currentString.IndexOfIgnore("Not");
                currentExpression.Index = idx;
                currentExpression.Operator = ExpressionFieldOperator.Not;

                proposedString = currentString.Substring(idx + 3, currentString.Length - idx - 3).TrimStart();
            }
        }

        private void SearchBitshiftOperators()
        {

        }

        private void SearchTextFunctions(
            string currentString,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (LeftParser.LeftRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || LeftParser.LeftRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var leftParser = new LeftParser(currentString, ExpressionFieldOperator.Left, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                leftParser.Parse();
                proposedString = leftParser.GetProposedString();
            }

            if (FormatCurrencyParser.FormatCurrencyRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatCurrencyParser.FormatCurrencyRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var fcParser = new FormatCurrencyParser(currentString, ExpressionFieldOperator.FormatCurrency, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                fcParser.Parse();
                proposedString = fcParser.GetProposedString();
            }
        }

        private void SearchDateTimeFunctions(
            string currentString,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (MonthNameParser.MonthNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MonthNameParser.MonthNameRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var mnParser = new MonthNameParser(currentString, ExpressionFieldOperator.MonthName, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                mnParser.Parse();
                proposedString = mnParser.GetProposedString();
            }

            if (DateAddParser.DateAddRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateAddParser.DateAddRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dateAddParser = new DateAddParser(currentString, ExpressionFieldOperator.DateAdd, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dateAddParser.Parse();
                proposedString = dateAddParser.GetProposedString();
            }

            if (DateDiffParser.DateDiffRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateDiffParser.DateDiffRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dateDiffParser = new DateDiffParser(currentString, ExpressionFieldOperator.DateDiff, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dateDiffParser.Parse();
                proposedString = dateDiffParser.GetProposedString();
            }

            if (DayParser.DayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DayParser.DayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dayParser = new DayParser(currentString, ExpressionFieldOperator.Day, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dayParser.Parse();
                proposedString = dayParser.GetProposedString();
            }

            if (NowParser.NowRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || NowParser.NowRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var nowParser = new NowParser(currentString, ExpressionFieldOperator.Now, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                nowParser.Parse();
                proposedString = nowParser.GetProposedString();
            }

            if (DateValueParser.DateValueRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateValueParser.DateValueRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dvParser = new DateValueParser(currentString, ExpressionFieldOperator.DateValue, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dvParser.Parse();
                proposedString = dvParser.GetProposedString();
            }

            if (FormatDateTimeParser.DateFormatRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatDateTimeParser.DateFormatRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dfParser = new FormatDateTimeParser(currentString, ExpressionFieldOperator.DateFormat, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dfParser.Parse();
                proposedString = dfParser.GetProposedString();
            }

            if (HourParser.HourRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || HourParser.HourRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var hourParser = new HourParser(currentString, ExpressionFieldOperator.Hour, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                hourParser.Parse();
                proposedString = hourParser.GetProposedString();
            }

            if (MinuteParser.MinuteRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MinuteParser.MinuteRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var minuteParser = new MinuteParser(currentString, ExpressionFieldOperator.Minute, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                minuteParser.Parse();
                proposedString = minuteParser.GetProposedString();
            }

            if (MonthParser.MonthRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MonthParser.MonthRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var monthParser = new MonthParser(currentString, ExpressionFieldOperator.Month, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                monthParser.Parse();
                proposedString = monthParser.GetProposedString();
            }

            if (SecondParser.SecondRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || SecondParser.SecondRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var secondParser = new SecondParser(currentString, ExpressionFieldOperator.Second, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                secondParser.Parse();
                proposedString = secondParser.GetProposedString();
            }
        }

        private void SearchMathFunctions()
        {

        }

        private void SearchInspectionFunctions(
            string currentString,
            ReportExpression currentExpression,
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
                (currentExpression.Operator == ExpressionFieldOperator.None || IsNothingParser.IsNothingRegex.Match(currentString).Index < currentExpression.Index))
            {
                var isNothingParser = new IsNothingParser(currentString, ExpressionFieldOperator.IsNothing, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                isNothingParser.Parse();
                proposedString = isNothingParser.GetProposedString();
            }
        }

        private void SearchProgramFlowFunctions(
            string currentString,
            ReportExpression currentExpression,
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
                (currentExpression.Operator == ExpressionFieldOperator.None || IfParser.IfRegex.Match(currentString).Index < currentExpression.Index))
            {
                var ifParser = new IfParser(currentString, ExpressionFieldOperator.If, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                ifParser.Parse();
                proposedString = ifParser.GetProposedString();
            }
        }

        private void SearchAggregateFunctions(
            string currentString,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (CountParser.CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || CountParser.CountRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var countParser = new CountParser(currentString, ExpressionFieldOperator.Count, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                countParser.Parse();
                proposedString = countParser.GetProposedString();
            }

            if (CountDistinctParser.CountDistinctRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || CountDistinctParser.CountDistinctRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var countDistinctParser = new CountDistinctParser(currentString, ExpressionFieldOperator.CountDistinct, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                countDistinctParser.Parse();
                proposedString = countDistinctParser.GetProposedString();
            }

            if (SumParser.SumRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || SumParser.SumRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var sumParser = new SumParser(currentString, ExpressionFieldOperator.Sum, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                sumParser.Parse();
                proposedString = sumParser.GetProposedString();
            }

            if (FirstParser.FirstRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || FirstParser.FirstRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                var firstParser = new FirstParser(currentString, ExpressionFieldOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
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
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref string proposedString
        )
        {
            if (RowNumberParser.RowNumberRegex.IsMatch(currentString) &&
               (currentExpression.Operator == ExpressionFieldOperator.None || RowNumberParser.RowNumberRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var rnParser = new RowNumberParser(currentString, ExpressionFieldOperator.RowNumber, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                rnParser.Parse();
                proposedString = rnParser.GetProposedString();
            }
        }

        private ReportExpression ParseReportExpressions(IEnumerable<ReportExpression> expressions, string requestedFormat)
        {
            // Last logical operator to be checked on boolean returning operator/function.
            ExpressionFieldOperator lastLogicalOperator = ExpressionFieldOperator.None;
            bool lastLogicalOperatorValue = false;

            if (expressions.Count() > 0)
            {
                var final = expressions.Aggregate((prev, next) =>
                {
                    var newExpr = new ReportExpression();

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

        private void ArithmeticOperatorAggregator(ReportExpression prev, ReportExpression next, ReportExpression newExpr)
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
                case ExpressionFieldOperator.Add:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") + double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.Subtract:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") - double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.Multiply:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") * double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.Divide:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") / double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.Mod:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") % double.Parse(next.Value?.ToString() ?? "");
                    break;
            }
        }

        private void ComparisonOperatorAggregator(ReportExpression prev, ReportExpression next, ReportExpression newExpr, ExpressionFieldOperator lastLogicalOperator, ref bool booleanOperator)
        {
            if (ComparisonOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = prev.Value;
            }

            switch (prev.Operator)
            {
                case ExpressionFieldOperator.LessThan:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") < double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.LessThanEqualTo:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") <= double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.GreaterThan:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") > double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.GreaterThanEqualTo:
                    newExpr.Value = double.Parse(prev.Value?.ToString() ?? "") >= double.Parse(next.Value?.ToString() ?? "");
                    break;
                case ExpressionFieldOperator.Equals:
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
                case ExpressionFieldOperator.NotEqual:
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

        private void LogicalOperatorAggregator(ReportExpression prev, ReportExpression next, ReportExpression newExpr, bool booleanOperator, ref ExpressionFieldOperator lastLogicalOperator)
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

                if (prev.Operator == ExpressionFieldOperator.Not && next.ResolvedType == typeof(bool))
                {
                    // Negate next value
                    newExpr.Value = !(bool)next.Value;
                }
            }
        }

        private void ConcatenationOperatorAggregator(ReportExpression prev, ReportExpression next, ReportExpression newExpr)
        {
            if (ConcatenationOperators.Contains(next.Operator))
            {
                newExpr.Operator = next.Operator;
                newExpr.Value = prev.Value;
            }

            switch (prev.Operator)
            {
                case ExpressionFieldOperator.ConcatAnd:
                    newExpr.Value = prev.Value.ToString() + next.Value.ToString();
                    break;
                case ExpressionFieldOperator.ConcatPlus:
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

        private bool EvaluateLogicalOperator(ExpressionFieldOperator lastLogicalOperator, bool valueA, bool valueB)
        {
            switch (lastLogicalOperator)
            {
                case ExpressionFieldOperator.And:
                case ExpressionFieldOperator.AndAlso:
                    return valueA && valueB;
                case ExpressionFieldOperator.Not:
                    return !valueA;
                case ExpressionFieldOperator.Or:
                case ExpressionFieldOperator.OrElse:
                    return valueA || valueB;
                case ExpressionFieldOperator.Xor:
                    return valueA ^ valueB;
            }

            return valueA;
        }

        private ReportExpression EvaluateRequestedFormat(ReportExpression finalExpression, string requestedFormat)
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
