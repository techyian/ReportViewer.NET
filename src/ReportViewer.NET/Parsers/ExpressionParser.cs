using DynamicExpresso;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers.Aggregate;
using ReportViewer.NET.Parsers.BuiltInFields;
using ReportViewer.NET.Parsers.Conversion;
using ReportViewer.NET.Parsers.DateAndTime;
using ReportViewer.NET.Parsers.Inspection;
using ReportViewer.NET.Parsers.Math;
using ReportViewer.NET.Parsers.Misc;
using ReportViewer.NET.Parsers.ProgramFlow;
using ReportViewer.NET.Parsers.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
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

            tablixText = WebUtility.HtmlDecode(tablixText);

            string currentString = tablixText.TrimStart('=').TrimStart();
            List<ReportExpression> expressions = new List<ReportExpression>();
            BaseParser nextParser = null;

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new ReportExpression();
                var proposedString = string.Empty;

                this.SearchBuiltInFields(currentString, currentExpression, ref nextParser);
                this.SearchAggregateFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchArithmeticOperators(currentString, currentExpression, expressions, ref proposedString);
                this.SearchComparisonOperators(currentString, currentExpression, ref proposedString);
                this.SearchProgramFlowFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchInspectionFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchLogicalOperators(currentString, currentExpression, ref proposedString);
                this.SearchTextFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchDateTimeFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchMathFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchMiscFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);
                this.SearchConversionFunctions(currentString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ref nextParser);

                if (FieldParser.FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || FieldParser.FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    nextParser = new FieldParser(currentString, ExpressionFieldOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                    
                }

                if (ParameterParser.ParameterRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || ParameterParser.ParameterRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    nextParser = new ParameterParser(currentString, ExpressionFieldOperator.Parameter, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                    
                }

                if (ArithmeticOperators.Contains(currentExpression.Operator) ||
                    ComparisonOperators.Contains(currentExpression.Operator) ||
                    ConcatenationOperators.Contains(currentExpression.Operator) ||
                    LogicalOperators.Contains(currentExpression.Operator))
                {
                    nextParser = null;
                }

                if (nextParser != null)
                {
                    nextParser.Parse();
                    proposedString = nextParser.GetProposedString();
                    nextParser = null;
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
                    else
                    {
                        // Dump out expression until next resolved function.
                        //currentExpression.ResolvedType = typeof(string);

                        if (currentExpression.Index > -1)
                        {
                            currentExpression.Value = currentString.Substring(0, currentExpression.Index);
                            proposedString = currentString.Substring(currentExpression.Index, currentString.Length - currentExpression.Index);
                        }
                        else
                        {
                            currentExpression.Value = currentString;
                            proposedString = "";
                        }

                        // See if DynamicExpresso can parse.
                        object dynFound = null;
                        var interpreter = new Interpreter();
                        try
                        {
                            dynFound = interpreter.Eval(currentExpression.Value.ToString());
                        }
                        catch
                        {
                        }

                        if (dynFound == null)
                        {                            
                            currentExpression.ResolvedType = typeof(object);                            
                        }
                        else
                        {
                            currentExpression.ResolvedType = dynFound.GetType();
                            currentExpression.Value = dynFound;
                        }

                        currentExpression.Operator = ExpressionFieldOperator.None;
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
            ref BaseParser nextParser
            )
        {
            if (ExecutionTimeParser.ExecutionTimeRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ExecutionTimeParser.ExecutionTimeRegex.Match(currentString).Index < currentExpression.Index))
            {
                nextParser = new ExecutionTimeParser(currentString, ExpressionFieldOperator.ExecutionTime, currentExpression, null, null, 0, null, _report);                
            }

            if (LanguageParser.LanguageRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || LanguageParser.LanguageRegex.Match(currentString).Index < currentExpression.Index))
            {
                nextParser = new LanguageParser(currentString, ExpressionFieldOperator.Language, currentExpression, null, null, 0, null, _report);                
            }

            if (ReportNameParser.ReportNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ReportNameParser.ReportNameRegex.Match(currentString).Index < currentExpression.Index))
            {
                nextParser = new ReportNameParser(currentString, ExpressionFieldOperator.ReportName, currentExpression, null, null, 0, null, _report);                
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

                if (lastExpression != null && lastExpression.Value != null)
                {
                    currentExpression.Index = idx;
                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1).TrimStart();
                    currentExpression.Operator = ExpressionFieldOperator.Subtract;
                }

                //if ((lastExpression != null && lastExpression.Value == null) || lastExpression == null)
                //{                    
                //    currentExpression.Operator = ExpressionFieldOperator.Negative;
                //}
                //else
                //{                    
                //    currentExpression.Operator = ExpressionFieldOperator.Subtract;
                //}
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
                ((currentString.IndexOfIgnore("<=") > -1 && currentString.IndexOfIgnore("=") < currentString.IndexOfIgnore("<=")) || currentString.IndexOfIgnore("<=") == -1) &&
                ((currentString.IndexOfIgnore(">=") > -1 && currentString.IndexOfIgnore("=") < currentString.IndexOfIgnore(">=")) || currentString.IndexOfIgnore(">=") == -1) &&
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
            ref BaseParser nextParser
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
            ref BaseParser nextParser
        )
        {
            if (AscParser.AscRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || AscParser.AscRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new AscParser(currentString, ExpressionFieldOperator.Asc, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }
            
            if (AscWParser.AscWRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || AscWParser.AscWRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new AscWParser(currentString, ExpressionFieldOperator.AscW, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (ChrParser.ChrRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ChrParser.ChrRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new ChrParser(currentString, ExpressionFieldOperator.Chr, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (ChrWParser.ChrWRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || ChrWParser.ChrWRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new ChrWParser(currentString, ExpressionFieldOperator.ChrW, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (FormatParser.FormatRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatParser.FormatRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new FormatParser(currentString, ExpressionFieldOperator.Format, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (FormatNumberParser.FormatNumberRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatNumberParser.FormatNumberRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new FormatNumberParser(currentString, ExpressionFieldOperator.FormatNumber, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
            }

            if (FormatPercentParser.FormatPercentRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatPercentParser.FormatPercentRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new FormatPercentParser(currentString, ExpressionFieldOperator.FormatPercent, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (GetCharParser.GetCharRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || GetCharParser.GetCharRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new GetCharParser(currentString, ExpressionFieldOperator.GetChar, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (InStrParser.InStrRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || InStrParser.InStrRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new InStrParser(currentString, ExpressionFieldOperator.InStr, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (InStrRevParser.InStrRevRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || InStrRevParser.InStrRevRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new InStrRevParser(currentString, ExpressionFieldOperator.InStrRev, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (LeftParser.LeftRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || LeftParser.LeftRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new LeftParser(currentString, ExpressionFieldOperator.Left, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (FormatCurrencyParser.FormatCurrencyRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatCurrencyParser.FormatCurrencyRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new FormatCurrencyParser(currentString, ExpressionFieldOperator.FormatCurrency, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
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
            ref BaseParser nextParser
        )
        {
            if (CDateParser.CDateRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CDateParser.CDateRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new CDateParser(currentString, ExpressionFieldOperator.CDate, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);
            }

            if (MonthNameParser.MonthNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MonthNameParser.MonthNameRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new MonthNameParser(currentString, ExpressionFieldOperator.MonthName, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DateAddParser.DateAddRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateAddParser.DateAddRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DateAddParser(currentString, ExpressionFieldOperator.DateAdd, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DateDiffParser.DateDiffRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateDiffParser.DateDiffRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DateDiffParser(currentString, ExpressionFieldOperator.DateDiff, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DatePartParser.DatePartRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DatePartParser.DatePartRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DatePartParser(currentString, ExpressionFieldOperator.DatePart, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DateSerialParser.DateSerialRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateSerialParser.DateSerialRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DateSerialParser(currentString, ExpressionFieldOperator.DateSerial, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DateStringParser.DateStringRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateStringParser.DateStringRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DateStringParser(currentString, ExpressionFieldOperator.DateString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DayParser.DayRegex.IsMatch(currentString) &&
                ((WeekdayParser.WeekdayRegex.IsMatch(currentString) && DayParser.DayRegex.Match(currentString).Index < WeekdayParser.WeekdayRegex.Match(currentString).Index) || !WeekdayParser.WeekdayRegex.IsMatch(currentString)) &&
                ((TodayParser.TodayRegex.IsMatch(currentString) && TodayParser.TodayRegex.Match(currentString).Index < TodayParser.TodayRegex.Match(currentString).Index) || !TodayParser.TodayRegex.IsMatch(currentString)) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DayParser.DayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DayParser(currentString, ExpressionFieldOperator.Day, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (NowParser.NowRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || NowParser.NowRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new NowParser(currentString, ExpressionFieldOperator.Now, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (DateValueParser.DateValueRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || DateValueParser.DateValueRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new DateValueParser(currentString, ExpressionFieldOperator.DateValue, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (FormatDateTimeParser.DateFormatRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || FormatDateTimeParser.DateFormatRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new FormatDateTimeParser(currentString, ExpressionFieldOperator.DateFormat, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (HourParser.HourRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || HourParser.HourRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new HourParser(currentString, ExpressionFieldOperator.Hour, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (MinuteParser.MinuteRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MinuteParser.MinuteRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new MinuteParser(currentString, ExpressionFieldOperator.Minute, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (MonthParser.MonthRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || MonthParser.MonthRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new MonthParser(currentString, ExpressionFieldOperator.Month, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (SecondParser.SecondRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || SecondParser.SecondRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new SecondParser(currentString, ExpressionFieldOperator.Second, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TimeOfDayParser.TimeOfDayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeOfDayParser.TimeOfDayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TimeOfDayParser(currentString, ExpressionFieldOperator.TimeOfDay, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TimerParser.TimerRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimerParser.TimerRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TimerParser(currentString, ExpressionFieldOperator.Timer, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TimeSerialParser.TimeSerialRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeSerialParser.TimeSerialRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TimeSerialParser(currentString, ExpressionFieldOperator.TimeSerial, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TimeStringParser.TimeStringRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeStringParser.TimeStringRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TimeStringParser(currentString, ExpressionFieldOperator.TimeString, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TimeValueParser.TimeValueRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TimeValueParser.TimeValueRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TimeValueParser(currentString, ExpressionFieldOperator.TimeValue, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (TodayParser.TodayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || TodayParser.TodayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new TodayParser(currentString, ExpressionFieldOperator.Today, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (WeekdayParser.WeekdayRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || WeekdayParser.WeekdayRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new WeekdayParser(currentString, ExpressionFieldOperator.Weekday, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (WeekdayNameParser.WeekdayNameRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || WeekdayNameParser.WeekdayNameRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new WeekdayNameParser(currentString, ExpressionFieldOperator.WeekdayName, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (YearParser.YearRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || YearParser.YearRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new YearParser(currentString, ExpressionFieldOperator.Year, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }
        }

        private void SearchMathFunctions(
            string currentString,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref BaseParser nextParser
        )
        {
            if (RoundParser.RoundRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || RoundParser.RoundRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new RoundParser(currentString, ExpressionFieldOperator.Round, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }
        }

        private void SearchInspectionFunctions(
            string currentString,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ref BaseParser nextParser
        )
        {
            // IsArray
            // IsDate
            // IsNothing
            // IsNumeric

            if (IsNothingParser.IsNothingRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || IsNothingParser.IsNothingRegex.Match(currentString).Index < currentExpression.Index))
            {
                nextParser = new IsNothingParser(currentString, ExpressionFieldOperator.IsNothing, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
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
            ref BaseParser nextParser
        )
        {
            // IIF
            // Choose
            // Switch

            if (IfParser.IfRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || IfParser.IfRegex.Match(currentString).Index < currentExpression.Index))
            {
                nextParser = new IfParser(currentString, ExpressionFieldOperator.If, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
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
            ref BaseParser nextParser
        )
        {
            if (CountParser.CountRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || CountParser.CountRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                nextParser = new CountParser(currentString, ExpressionFieldOperator.Count, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (CountDistinctParser.CountDistinctRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || CountDistinctParser.CountDistinctRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                nextParser = new CountDistinctParser(currentString, ExpressionFieldOperator.CountDistinct, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (SumParser.SumRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || SumParser.SumRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                nextParser = new SumParser(currentString, ExpressionFieldOperator.Sum, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (FirstParser.FirstRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == ExpressionFieldOperator.None || FirstParser.FirstRegex.Match(currentString).Index < currentExpression.Index)
                )
            {
                nextParser = new FirstParser(currentString, ExpressionFieldOperator.Field, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
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
            ref BaseParser nextParser
        )
        {
            if (CBoolParser.CBoolRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CBoolParser.CBoolRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new CBoolParser(currentString, ExpressionFieldOperator.CBool, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (CCharParser.CCharRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CCharParser.CCharRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new CCharParser(currentString, ExpressionFieldOperator.CChar, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (CIntParser.CIntRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CIntParser.CIntRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new CIntParser(currentString, ExpressionFieldOperator.CInt, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }

            if (CDecParser.CDecRegex.IsMatch(currentString) &&
                (currentExpression.Operator == ExpressionFieldOperator.None || CDecParser.CDecRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new CDecParser(currentString, ExpressionFieldOperator.CDec, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
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
            ref BaseParser nextParser
        )
        {
            if (RowNumberParser.RowNumberRegex.IsMatch(currentString) &&
               (currentExpression.Operator == ExpressionFieldOperator.None || RowNumberParser.RowNumberRegex.Match(currentString).Index < currentExpression.Index)
            )
            {
                nextParser = new RowNumberParser(currentString, ExpressionFieldOperator.RowNumber, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, _report);                
            }
        }

        private object ParseReportExpressions(IEnumerable<ReportExpression> expressions, string requestedFormat)
        {           
            if (expressions.Count() > 0)
            {
                var finalExpr = new StringBuilder();
                var interpreterParams = new List<Parameter>();
                var indx = 0;
                var forceString = expressions.Any(ex => ex.Value != null && ex.ResolvedType == typeof(string));

                foreach (var exp in expressions)
                {
                    if (exp.Operator == ExpressionFieldOperator.Negative)
                    {
                        continue;
                    }

                    if (exp.Value == null &&
                        !ArithmeticOperators.Contains(exp.Operator) &&
                        !ComparisonOperators.Contains(exp.Operator) &&
                        !LogicalOperators.Contains(exp.Operator) &&
                        !ConcatenationOperators.Contains(exp.Operator))
                    {
                        finalExpr.Clear();
                        break;
                    }

                    if (exp.Value != null && 
                        !ArithmeticOperators.Contains(exp.Operator) &&
                        !ComparisonOperators.Contains(exp.Operator) && 
                        !LogicalOperators.Contains(exp.Operator) && 
                        !ConcatenationOperators.Contains(exp.Operator))
                    {
                        var param = $"exp{indx}";

                        if (finalExpr.Length > 0)
                        {
                            finalExpr.Append(" ");
                        }

                        if (exp.Value.ToString() == "True" || exp.Value.ToString() == "False")
                        {
                            interpreterParams.Add(new Parameter(param, typeof(bool), exp.Value.ToString() == "True"));
                            finalExpr.Append(param);
                        }
                        else if (forceString)
                        {                                                        
                            interpreterParams.Add(new Parameter(param, typeof(string), exp.Value.ToString()));
                            finalExpr.Append(param);
                        }
                        else if (exp.ResolvedType == typeof(object))
                        {                            
                            finalExpr.Append(exp.Value.ToString());                                                        
                        }
                        else
                        {                            
                            interpreterParams.Add(new Parameter(param, exp.ResolvedType, exp.Value));
                            finalExpr.Append(param);
                        }                                                                        
                    }

                    finalExpr.Append(this.ParseArithmeticOperator(exp));
                    finalExpr.Append(this.ParseComparisonOperator(exp));
                    finalExpr.Append(this.ParseLogicalOperator(exp));
                    finalExpr.Append(this.ParseConcatenationOperator(exp));

                    indx++;
                }

                if (finalExpr.Length == 0)
                {
                    return null;
                }

                try
                {
                    var interpreter = new Interpreter();
                    var result = interpreter.Eval(finalExpr.ToString(), interpreterParams.ToArray());

                    return EvaluateRequestedFormat(result, requestedFormat);
                }
                catch (Exception e)
                {
                    return "Could not parse, please check expression.";
                }
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
            if (string.IsNullOrEmpty(requestedFormat) || finalExpr == null)
                return finalExpr;

            switch (Type.GetTypeCode(finalExpr.GetType()))
            {
                case TypeCode.String:
                    return finalExpr;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    return long.Parse(finalExpr.ToString()).ToString(requestedFormat);
                case TypeCode.Decimal:
                    return decimal.Parse(finalExpr.ToString()).ToString(requestedFormat);
                case TypeCode.Double:
                    return double.Parse(finalExpr.ToString()).ToString(requestedFormat);
                case TypeCode.DateTime:
                    return DateTime.Parse(finalExpr.ToString()).ToString(requestedFormat);
            }

            return finalExpr;
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
