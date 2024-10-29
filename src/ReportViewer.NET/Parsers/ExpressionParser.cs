using DynamicExpresso;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers.Aggregate;
using ReportViewer.NET.Parsers.BuiltInFields;
using ReportViewer.NET.Parsers.Conversion;
using ReportViewer.NET.Parsers.DateAndTime;
using ReportViewer.NET.Parsers.Inspection;
using ReportViewer.NET.Parsers.Misc;
using ReportViewer.NET.Parsers.ProgramFlow;
using ReportViewer.NET.Parsers.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

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

            return ParseReportExpressions(expressions, requestedFormat);
        }

        public string ParseReportExpressionStringToDisplayCultureSpecific(
            string tablixText,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            string requestedFormat
        )
        {
            return Convert.ToString(this.ParseReportExpressionString(tablixText, dataSetResults, values, currentRowNumber, dataSets, activeDataset, requestedFormat), CultureInfo.CurrentCulture);
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
                this.SearchArithmeticOperators(currentString, currentExpression, expressions, ref proposedString);
                this.SearchComparisonOperators(currentString, currentExpression, ref proposedString);
                this.SearchProgramFlowFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchInspectionFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchLogicalOperators(currentString, currentExpression, ref proposedString);
                this.SearchTextFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchDateTimeFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchMiscFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);
                this.SearchConversionFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref proposedString);

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
                    // What we've been left with isn't in a string literal. First try to unwrap any nested parenthesis before attempting parsing.
                    else if (currentString.StartsWith('('))
                    {                        
                        var openingParens = 1;
                        var openingIndx = currentString.IndexOf("(");
                        var closingIdx = -1;

                        for (var i = openingIndx + 1; i < currentString.Length; i++)
                        {
                            if (currentString[i] == '(')
                            {
                                openingParens++;
                            }

                            if (currentString[i] == ')')
                            {
                                openingParens--;
                            }

                            if (openingParens == 0)
                            {
                                closingIdx = i;
                            }
                        }

                        if (closingIdx == -1)
                        {
                            throw new InvalidOperationException("Expression parenthesis mismatch. Check data.");
                        }

                        var nestedStr = currentString.Substring(openingIndx + 1, closingIdx - (openingIndx + 1));                       
                        var nestedExpr = this.ParseReportExpressionString(nestedStr, dataSetResults, values, currentRowNumber, dataSets, activeDataset, null);

                        this.TryExtractExpression(nestedExpr.ExpressionAsString(), currentExpression, expressions, out var ps);

                        currentExpression.NestedParenthesis = true;

                        proposedString = ps;
                    }
                    else
                    {
                        this.TryExtractExpression(currentString, currentExpression, expressions, out var ps);

                        proposedString = ps;
                    }
                }

                expressions.Add(currentExpression);
                currentString = proposedString;
            }

            return expressions;
        }

        private bool TryExtractExpression(string currentString, ReportExpression currentExpression, List<ReportExpression> currentExpressions, out string proposedString)
        {
            // When looking for remaining numerical expressions, first check for signed integer, then double for floating point and finally dump out to string.
            // We're only interested in decimal if either we've received a decimal from DB or user has explicitly casted using CDec.
            var lastExpression = currentExpressions.LastOrDefault();
            var found = false;
            proposedString = string.Empty;

            if (long.TryParse(currentString, out var parsedLong))
            {
                // We found an integer value, we can parse this and get out of the loop.
                currentExpression.ResolvedType = typeof(long);
                currentExpression.Operator = ExpressionFieldOperator.None;

                if (lastExpression != null && lastExpression.Operator == ExpressionFieldOperator.Negative)
                {
                    currentExpression.Value = -parsedLong;
                    currentExpressions.Remove(lastExpression);
                }
                else
                {
                    currentExpression.Value = parsedLong;
                }

                found = true;
            }            
            else if (double.TryParse(currentString, CultureInfo.InvariantCulture, out var parsedDouble))
            {
                // We found a double value, we can parse this and get out of the loop.
                currentExpression.ResolvedType = typeof(double);
                currentExpression.Operator = ExpressionFieldOperator.None;

                if (lastExpression != null && lastExpression.Operator == ExpressionFieldOperator.Negative)
                {
                    currentExpression.Value = -parsedDouble;
                    currentExpressions.Remove(lastExpression);
                }
                else
                {
                    currentExpression.Value = parsedDouble;
                }

                found = true;
            }
            else
            {
                // Take char by char and see if we can extract anything useful. If not, dump to a string and get out of loop.
                var split = currentString.TrimStart().Split(' ');
                
                for (var i = 0; i < split.Length; i++)
                {
                    // Add in other possibilities? What about boolean expression?
                    if (long.TryParse(split[i], out var parsedSplitLong))
                    {
                        currentExpression.ResolvedType = typeof(long);
                        currentExpression.Operator = ExpressionFieldOperator.None;

                        if (lastExpression != null && lastExpression.Operator == ExpressionFieldOperator.Negative)
                        {
                            currentExpression.Value = -parsedSplitLong;
                            currentExpressions.Remove(lastExpression);
                        }
                        else
                        {
                            currentExpression.Value = parsedSplitLong;
                        }

                        currentExpression.Index = currentString.IndexOf(split[i]);
                        proposedString = currentString.Substring(currentExpression.Index + parsedSplitLong.ToString().Length, currentString.Length - currentExpression.Index - parsedSplitLong.ToString().Length);

                        // Clear up any whitespace not removed by statement above.
                        proposedString = proposedString.TrimStart();

                        found = true;
                    
                        break;
                    }                    
                    else if (double.TryParse(split[i], CultureInfo.InvariantCulture, out var parsedSplitDouble))
                    {
                        currentExpression.ResolvedType = typeof(double);
                        currentExpression.Operator = ExpressionFieldOperator.None;

                        if (lastExpression != null && lastExpression.Operator == ExpressionFieldOperator.Negative)
                        {
                            currentExpression.Value = -parsedSplitDouble;
                            currentExpressions.Remove(lastExpression);
                        }
                        else
                        {
                            currentExpression.Value = parsedSplitDouble;
                        }

                        currentExpression.Index = currentString.IndexOf(split[i]);
                        proposedString = currentString.Substring(currentExpression.Index + parsedSplitDouble.ToString().Length, currentString.Length - currentExpression.Index - parsedSplitDouble.ToString().Length);

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
                }
            }

            return found;
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
            List<ReportExpression> currentExpressions,
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
                var lastExpression = currentExpressions.LastOrDefault();

                currentExpression.Index = idx;
                proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();

                if ((lastExpression != null && lastExpression.Value == null) || lastExpression == null)
                {                    
                    currentExpression.Operator = ExpressionFieldOperator.Negative;
                }
                else
                {                    
                    currentExpression.Operator = ExpressionFieldOperator.Subtract;
                }
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
            if (AscParser.AscRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || AscParser.AscRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var ascParser = new AscParser(currentString, ExpressionFieldOperator.Asc, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                ascParser.Parse();
                proposedString = ascParser.GetProposedString();
            }
            
            if (AscWParser.AscWRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || AscWParser.AscWRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var ascWParser = new AscWParser(currentString, ExpressionFieldOperator.AscW, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                ascWParser.Parse();
                proposedString = ascWParser.GetProposedString();
            }

            if (ChrParser.ChrRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ChrParser.ChrRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var chrParser = new ChrParser(currentString, ExpressionFieldOperator.Chr, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                chrParser.Parse();
                proposedString = chrParser.GetProposedString();
            }

            if (ChrWParser.ChrWRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ChrWParser.ChrWRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var chrWParser = new ChrWParser(currentString, ExpressionFieldOperator.ChrW, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                chrWParser.Parse();
                proposedString = chrWParser.GetProposedString();
            }

            if (FormatParser.FormatRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatParser.FormatRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var formatParser = new FormatParser(currentString, ExpressionFieldOperator.Format, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                formatParser.Parse();
                proposedString = formatParser.GetProposedString();
            }

            if (FormatNumberParser.FormatNumberRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatNumberParser.FormatNumberRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var formatNumberParser = new FormatNumberParser(currentString, ExpressionFieldOperator.FormatNumber, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                formatNumberParser.Parse();
                proposedString = formatNumberParser.GetProposedString();
            }

            if (FormatPercentParser.FormatPercentRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatPercentParser.FormatPercentRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var formatPercentParser = new FormatPercentParser(currentString, ExpressionFieldOperator.FormatPercent, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                formatPercentParser.Parse();
                proposedString = formatPercentParser.GetProposedString();
            }

            if (GetCharParser.GetCharRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || GetCharParser.GetCharRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var getCharParser = new GetCharParser(currentString, ExpressionFieldOperator.GetChar, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                getCharParser.Parse();
                proposedString = getCharParser.GetProposedString();
            }

            if (InStrParser.InStrRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || InStrParser.InStrRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var inStrParser = new InStrParser(currentString, ExpressionFieldOperator.InStr, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                inStrParser.Parse();
                proposedString = inStrParser.GetProposedString();
            }

            if (InStrRevParser.InStrRevRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || InStrRevParser.InStrRevRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var inStrRevParser = new InStrRevParser(currentString, ExpressionFieldOperator.InStrRev, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                inStrRevParser.Parse();
                proposedString = inStrRevParser.GetProposedString();
            }

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
            if (CDateParser.CDateRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CDateParser.CDateRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var cDateParser = new CDateParser(currentString, ExpressionFieldOperator.CDate, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                cDateParser.Parse();
                proposedString = cDateParser.GetProposedString();
            }

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

            if (DatePartParser.DatePartRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DatePartParser.DatePartRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var datePartParser = new DatePartParser(currentString, ExpressionFieldOperator.DatePart, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                datePartParser.Parse();
                proposedString = datePartParser.GetProposedString();
            }

            if (DateSerialParser.DateSerialRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateSerialParser.DateSerialRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dateSerialParser = new DateSerialParser(currentString, ExpressionFieldOperator.DateSerial, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dateSerialParser.Parse();
                proposedString = dateSerialParser.GetProposedString();
            }

            if (DateStringParser.DateStringRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateStringParser.DateStringRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var dateStringParser = new DateStringParser(currentString, ExpressionFieldOperator.DateString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                dateStringParser.Parse();
                proposedString = dateStringParser.GetProposedString();
            }

            if (DayParser.DayRegex.IsMatch(currentString) &&
                ((WeekdayParser.WeekdayRegex.IsMatch(currentString) && DayParser.DayRegex.Match(currentString).Index < WeekdayParser.WeekdayRegex.Match(currentString).Index) || !WeekdayParser.WeekdayRegex.IsMatch(currentString)) &&
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

            if (TimeOfDayParser.TimeOfDayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeOfDayParser.TimeOfDayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var todParser = new TimeOfDayParser(currentString, ExpressionFieldOperator.TimeOfDay, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                todParser.Parse();
                proposedString = todParser.GetProposedString();
            }

            if (TimerParser.TimerRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimerParser.TimerRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var timerParser = new TimerParser(currentString, ExpressionFieldOperator.Timer, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                timerParser.Parse();
                proposedString = timerParser.GetProposedString();
            }

            if (TimeSerialParser.TimeSerialRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeSerialParser.TimeSerialRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var timeSerialParser = new TimeSerialParser(currentString, ExpressionFieldOperator.TimeSerial, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                timeSerialParser.Parse();
                proposedString = timeSerialParser.GetProposedString();
            }

            if (TimeStringParser.TimeStringRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeStringParser.TimeStringRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var timeStringParser = new TimeStringParser(currentString, ExpressionFieldOperator.TimeString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                timeStringParser.Parse();
                proposedString = timeStringParser.GetProposedString();
            }

            if (TimeValueParser.TimeValueRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeValueParser.TimeValueRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var timeValueParser = new TimeValueParser(currentString, ExpressionFieldOperator.TimeValue, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                timeValueParser.Parse();
                proposedString = timeValueParser.GetProposedString();
            }

            if (TodayParser.TodayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TodayParser.TodayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var todayParser = new TodayParser(currentString, ExpressionFieldOperator.Today, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                todayParser.Parse();
                proposedString = todayParser.GetProposedString();
            }

            if (WeekdayParser.WeekdayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || WeekdayParser.WeekdayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var weekdayParser = new WeekdayParser(currentString, ExpressionFieldOperator.Weekday, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                weekdayParser.Parse();
                proposedString = weekdayParser.GetProposedString();
            }

            if (WeekdayNameParser.WeekdayNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || WeekdayNameParser.WeekdayNameRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var weekdayNameParser = new WeekdayNameParser(currentString, ExpressionFieldOperator.WeekdayName, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                weekdayNameParser.Parse();
                proposedString = weekdayNameParser.GetProposedString();
            }

            if (YearParser.YearRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || YearParser.YearRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var yearParser = new YearParser(currentString, ExpressionFieldOperator.Year, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                yearParser.Parse();
                proposedString = yearParser.GetProposedString();
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

        private void SearchConversionFunctions(
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
            if (CIntParser.CIntRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CIntParser.CIntRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                var cIntParser = new CIntParser(currentString, ExpressionFieldOperator.CInt, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
                cIntParser.Parse();
                proposedString = cIntParser.GetProposedString();
            }
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

        private object ParseReportExpressions(IEnumerable<ReportExpression> expressions, string requestedFormat)
        {           
            if (expressions.Count() > 0)
            {
                var finalExpr = new StringBuilder();
                var interpreterParams = new List<Parameter>();

                foreach (var exp in expressions)
                {
                    if (exp.Operator == ExpressionFieldOperator.Negative)
                    {
                        continue;
                    }

                    if (exp.Value != null)
                    {
                        var param = RandomString(2);
                        interpreterParams.Add(new Parameter(param, exp.ResolvedType, exp.Value));
                        finalExpr.Append(param);
                    }

                    finalExpr.Append(this.ParseArithmeticOperator(exp));
                    finalExpr.Append(this.ParseComparisonOperator(exp));
                    finalExpr.Append(this.ParseLogicalOperator(exp));
                    finalExpr.Append(this.ParseConcatenationOperator(exp));
                }

                var interpreter = new Interpreter();
                var result = interpreter.Eval(finalExpr.ToString(), interpreterParams.ToArray());

                return EvaluateRequestedFormat(result, requestedFormat);
            }

            return null;
        }

        private string ParseArithmeticOperator(ReportExpression exp)
        {
            if (ArithmeticOperators.Contains(exp.Operator))
            {
                switch (exp.Operator)
                {
                    case ExpressionFieldOperator.Add:
                        return " + ";
                        
                    case ExpressionFieldOperator.Subtract:
                        return " - ";
                    case ExpressionFieldOperator.Multiply:
                        return " * ";
                    case ExpressionFieldOperator.Divide:
                        return " / ";
                    case ExpressionFieldOperator.Mod:
                        return " % ";
                }
            }

            return string.Empty;
        }

        private string ParseComparisonOperator(ReportExpression exp)
        {
            if (ComparisonOperators.Contains(exp.Operator))
            {
                switch (exp.Operator)
                {
                    case ExpressionFieldOperator.LessThan:
                        return " < ";
                    case ExpressionFieldOperator.LessThanEqualTo:
                        return " <= ";
                    case ExpressionFieldOperator.GreaterThan:
                        return " > ";
                    case ExpressionFieldOperator.GreaterThanEqualTo:
                        return " >= ";
                    case ExpressionFieldOperator.Equals:
                        return " == ";
                    case ExpressionFieldOperator.NotEqual:
                        return " != ";
                }
            }

            return string.Empty;
        }

        private string ParseLogicalOperator(ReportExpression exp)
        {
            if (LogicalOperators.Contains(exp.Operator))
            {
                switch (exp.Operator)
                {
                    case ExpressionFieldOperator.Not:
                        return " !";
                    case ExpressionFieldOperator.And:
                    case ExpressionFieldOperator.AndAlso:
                        return " && ";
                    case ExpressionFieldOperator.Or:
                    case ExpressionFieldOperator.OrElse:
                        return " || ";
                }
            }

            return string.Empty;
        }

        private string ParseConcatenationOperator(ReportExpression exp)
        {
            if (ConcatenationOperators.Contains(exp.Operator))
            {
                switch (exp.Operator)
                {
                    case ExpressionFieldOperator.ConcatAnd:
                    case ExpressionFieldOperator.ConcatPlus:
                        return " + ";                    
                }
            }

            return string.Empty;
        }

        private object EvaluateRequestedFormat(object finalExpr, string requestedFormat)
        {
            if (string.IsNullOrEmpty(requestedFormat))
                return finalExpr;

            if (DateTime.TryParse(finalExpr.ToString(), out var dtt))
            {
                return dtt.ToString(requestedFormat);
            }

            if (decimal.TryParse(finalExpr.ToString(), out var dec))
            {
                return dec.ToString(requestedFormat);
            }

            if (double.TryParse(finalExpr.ToString(), out var dbl))
            {
                return dbl.ToString(requestedFormat);
            }

            if (long.TryParse(finalExpr.ToString(), out var lng))
            {
                return lng.ToString(requestedFormat);
            }

            return finalExpr;
        }

        public static (Type, object) ExtractTypeFromValue(object value)
        {
            if (value == null)
            {
                return (typeof(object), null);
            }

            if (DateTime.TryParse(value.ToString(), out var dttValue))
            {
                return (typeof(DateTime), dttValue);
            }

            if (bool.TryParse(value.ToString(), out var bValue))
            {
                return (typeof(bool), bValue);
            }

            if (double.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var dValue))
            {
                return (typeof(double), dValue);
            }

            if (long.TryParse(value.ToString(), out var lValue))
            {
                return (typeof(long), lValue);
            }

            return (typeof(string), value);
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

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
