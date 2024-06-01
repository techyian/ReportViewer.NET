using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Textbox : ReportItem
    {   
        public bool CanGrow { get; set; }
        public bool KeepTogether { get; set; }
        public List<Paragraph> Paragraphs { get; set; }        
        public TablixCell Cell { get; set; }
        public IEnumerable<DataSet> DataSets { get; set; }

        public Textbox(TablixCell cell, XElement textbox, IEnumerable<DataSet> dataSets)
            : this(textbox, dataSets)
        {
            Cell = cell;
        }

        public Textbox(XElement textbox, IEnumerable<DataSet> dataSets)
            : base(textbox)
        {
            this.DataSets = dataSets;
            this.Paragraphs = new List<Paragraph>();

            this.Name = textbox.Attribute("Name")?.Value;
            this.CanGrow = textbox.Element(ReportItem.Namespace + "CanGrow")?.Value == "true";
            this.KeepTogether = textbox.Element(ReportItem.Namespace + "KeepTogether")?.Value == "true";

            var paragraphs = textbox.Elements(ReportItem.Namespace + "Paragraphs").Elements(ReportItem.Namespace + "Paragraph");
            
            if (paragraphs != null)
            {
                foreach (var p in paragraphs)
                {
                    Paragraphs.Add(new Paragraph(this, p));
                }
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<div {Style?.Build()}>");

            if (Paragraphs != null)
            {
                foreach (var p in Paragraphs)
                {
                    sb.AppendLine(p.Build());
                }
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }

    public class Paragraph
    {
        public List<TextRun> TextRuns { get; set; }
        public Style Style { get; set; }
        public Textbox Textbox { get; set; }

        public Paragraph(Textbox textbox, XElement paragraph)
        {
            Textbox = textbox;
            TextRuns = new List<TextRun>();

            var textRuns = paragraph.Elements(ReportItem.Namespace + "TextRuns").Elements(ReportItem.Namespace + "TextRun");

            if (textRuns != null)
            {
                foreach (var tr in textRuns)
                {
                    TextRuns.Add(new TextRun(this, tr));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<p {Style?.Build()}>");

            if (TextRuns != null)
            {
                foreach (var tr in TextRuns)
                {
                    sb.AppendLine(tr.Build());
                    sb.AppendLine("<span> </span>");
                }
            }

            sb.AppendLine("</p>");

            return sb.ToString();
        }
    }

    public class TextRun
    {
        public static Regex CountRegex = new Regex("(?:\\(*?)(?:Count?)(\\((.*?)\\)\\)*)");
        public static Regex FieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

        public string Value { get; set; }
        public Style Style { get; set; }
        public bool ContainsDataSetExpression { get; set; }
        public Paragraph Paragraph { get; set; }
        public string Format { get; set; }

        public TextRun(Paragraph paragraph, XElement textRun)
        {
            Paragraph = paragraph;
            Value = textRun.Element(ReportItem.Namespace + "Value")?.Value;
            Style = new Style(textRun.Element(ReportItem.Namespace + "Style"));
            Format = textRun.Element(ReportItem.Namespace + "Style").Element(ReportItem.Namespace + "Format")?.Value;
        }

        public string Build()
        {
            if (Value.StartsWith('='))
            {
                TablixCell cell = Paragraph.Textbox.Cell;

                if (cell != null)
                {
                    // We've come from a tablix cell.
                    if (cell.Row.Values != null)
                    {
                        var parsedValue = ParseTablixExpressionString(Value, cell.Row.Body.Tablix.DataSetReference?.DataSet?.DataSetResults, cell.Row.Values, null, Format);

                        return $"<span {Style?.Build()}>{parsedValue}</span>";
                    }
                }
                else
                {
                    // We've come from a standalone textbox. Try to find dataset for this field.
                    var parsedValue = ParseTablixExpressionString(Value, null, null, Paragraph.Textbox.DataSets, Format);

                    return $"<span {Style?.Build()}>{parsedValue}</span>";
                }
            }

            return $"<span {Style?.Build()}>{Value}</span>";
        }

        // TODO: Extract this into parser class.

        private string ParseTablixExpressionString(
            string tablixText,
            IEnumerable<dynamic> dataSetResults,
            dynamic values,
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

        private string ParseTablixExpression(IEnumerable<TablixExpression> expressions, string requestedFormat)
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

        private string EvaluateRequestedFormat(object value, Type resolvedType, string requestedFormat)
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

        private void ParseCountExpression(
            string currentString,
            TablixExpression expression,
            IEnumerable<dynamic> dataSetResults,
            dynamic values,
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
                var fieldEnd = matchString.IndexOf(".", fieldsIdx);
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

        private void ParseFieldExpression(
            string currentString,
            TablixExpression expression,
            IEnumerable<dynamic> dataSetResults,
            dynamic values,
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

        private (Type, object) ExtractExpressionValue(
            string dataSetName,
            string fieldName,
            TablixOperator op,
            IEnumerable<dynamic> dataSetResults,
            dynamic values,
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
                        var expando = (IDictionary<string, object>)values;
                        if (expando.ContainsKey(fieldName))
                        {
                            if (expando[fieldName] == null)
                            {
                                return (typeof(object), null);
                            }

                            return (expando[fieldName].GetType(), expando[fieldName]);
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
