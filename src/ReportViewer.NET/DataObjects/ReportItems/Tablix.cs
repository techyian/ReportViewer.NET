using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class ReportItemComparer : IComparer<ReportItem>
    {
        public int Compare(ReportItem? x, ReportItem? y)
        {
            if (x?.Top == y?.Top && x?.Left < y?.Left)
            {
                return -1;
            }

            if (x?.Top == y?.Top && x?.Left > y?.Left)
            {
                return 1;
            }

            if (x?.Top == y?.Top)
            {
                return 0;
            }

            if (x?.Top > y?.Top)
            {
                if (x?.Top > y?.Top + y?.Height)
                {
                    // New row.
                    return 1;
                }
                else
                {
                    // Same row.
                    if (x?.Left > y?.Left)
                    {
                        return 1;
                    }

                    return -1;
                }
            }
            else
            {
                if (y?.Top > x?.Top + x?.Height)
                {
                    return -1;
                }
                else
                {
                    // Same row.
                    if (y?.Left > x?.Left)
                    {
                        return -1;
                    }

                    return 1;
                }
            }
        }
    }

    public abstract class ReportItem
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public abstract string Build();
    }

    public class ReportRow
    {
        public double MaxTop { get; set; }
        public double MaxHeight { get; set; }
        public double MaxLeft { get; set; }
        public double MaxWidth { get; set; }
        public List<ReportItem> RowItems { get; set; } = new List<ReportItem>();
    }

    public class Tablix : ReportItem
    {
        public static XNamespace Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";

        public string Name { get; set; }
        public string DataSetName { get; set; }
        public DataSetReference DataSetReference { get; set; }
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }
        public Style Style { get; set; }
        public IEnumerable<DataSet> DataSets { get; set; }

        private TablixBody TablixBodyObj { get; set; }

        public Tablix(XElement tablix, IEnumerable<DataSet> datasets)
        {
            DataSets = datasets;
            TablixBodyObj = new TablixBody(this, tablix.Element(Namespace + "TablixBody"));

            DataSetName = tablix.Element(Namespace + "DataSetName")?.Value;
            Hidden = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "Hidden")?.Value == "true";
            ToggleItem = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "ToggleItem")?.Value;

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Top")?.Value) && double.TryParse(tablix.Element(Namespace + "Top").Value.TrimEnd('m'), out var top))
            {
                Top = top;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Left")?.Value) && double.TryParse(tablix.Element(Namespace + "Left").Value.TrimEnd('m'), out var left))
            {
                Left = left;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Width")?.Value) && double.TryParse(tablix.Element(Namespace + "Width").Value.TrimEnd('m'), out var width))
            {
                Width = width;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Height")?.Value) && double.TryParse(tablix.Element(Namespace + "Height").Value.TrimEnd('m'), out var height))
            {
                Height = height;
            }

            Style = new Style(tablix.Element(Namespace + "Style"));
            Style.Top = tablix.Element(Namespace + "Top")?.Value;
            Style.Left = tablix.Element(Namespace + "Left")?.Value;
            Style.Height = tablix.Element(Namespace + "Height")?.Value;
            Style.Width = tablix.Element(Namespace + "Width")?.Value;

            if (!string.IsNullOrEmpty(DataSetName))
            {
                DataSetReference = new DataSetReference()
                {
                    DataSetName = DataSetName
                };

                DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == DataSetReference.DataSetName);
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<table {Style?.Build()} class=\"table reportviewer-table\">");
            sb.AppendLine(TablixBodyObj?.Build());
            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }

    public class TablixBody
    {
        public List<TablixColumn> TablixColumns { get; set; }
        public List<TablixRow> TablixRows { get; set; }
        public Tablix Tablix { get; set; }

        internal TablixBody(Tablix tablix, XElement tablixBody)
        {
            Tablix = tablix;
            TablixColumns = new List<TablixColumn>();
            TablixRows = new List<TablixRow>();

            var columns = tablixBody.Elements(Tablix.Namespace + "TablixColumns").Elements(Tablix.Namespace + "TablixColumn");
            var rows = tablixBody.Elements(Tablix.Namespace + "TablixRows").Elements(Tablix.Namespace + "TablixRow");

            if (columns != null)
            {
                foreach (var c in columns)
                {
                    TablixColumns.Add(new TablixColumn(this, c));
                }
            }

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    TablixRows.Add(new TablixRow(this, r));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            foreach (var column in TablixColumns)
            {
                sb.AppendLine(column.Build());
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");

            for (var i = 0; i < TablixRows.Count; i++)
            {
                var row = TablixRows[i];

                if (row.ContainsRepeatExpression && row.Body.Tablix.DataSetReference != null && row.Body.Tablix.DataSetReference.DataSet.DataSetResults != null)
                {
                    foreach (var result in row.Body.Tablix.DataSetReference.DataSet.DataSetResults)
                    {
                        row.Values = result;

                        sb.AppendLine("<tr>");
                        sb.AppendLine(row.Build());
                        sb.AppendLine("</tr>");
                    }
                }
                else
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine(row.Build());
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");

            return sb.ToString();
        }
    }

    public class TablixColumn
    {
        public string Width { get; set; }
        public TablixBody Body { get; set; }

        internal TablixColumn(TablixBody body, XElement column)
        {
            Body = body;
            Width = column.Element(Tablix.Namespace + "Width")?.Value;
        }

        public string Build()
        {
            return @"<td width=""" + Width + @"""></td>";
        }
    }

    public class TablixRow
    {
        public string Height { get; set; }
        public List<TablixCell> TablixCells { get; set; }
        public TablixBody Body { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public dynamic Values { get; set; }

        internal TablixRow(TablixBody body, XElement row)
        {
            Body = body;
            Height = row.Element(Tablix.Namespace + "Height")?.Value;
            TablixCells = new List<TablixCell>();

            var cells = row.Elements(Tablix.Namespace + "TablixCells").Elements(Tablix.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    TablixCells.Add(new TablixCell(this, c));

                    if (!ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        ContainsRepeatExpression = !LayoutProvider.CountRegex.IsMatch(c.Value) && LayoutProvider.FieldRegex.IsMatch(c.Value);
                    }
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<tr height=""" + Height + @""">");

            if (TablixCells != null)
            {
                foreach (var cell in TablixCells)
                {
                    sb.AppendLine("<td>");
                    if (cell.TablixCellContent != null)
                    {
                        for (var i = 0; i < cell.TablixCellContent.Count; i++)
                        {
                            var content = cell.TablixCellContent[i];

                            sb.AppendLine(content.Build());
                        }
                    }
                    sb.AppendLine("</td>");
                }
            }

            sb.AppendLine("</tr>");

            return sb.ToString();
        }
    }

    public class TablixCell
    {
        public List<ReportItem> TablixCellContent { get; set; }
        public TablixRow Row { get; set; }

        internal TablixCell(TablixRow row, XElement cell)
        {
            Row = row;
            TablixCellContent = new List<ReportItem>();

            var cellContents = cell.Elements(Tablix.Namespace + "CellContents");

            if (cellContents != null)
            {
                foreach (var c in cellContents)
                {
                    var textboxes = c.Elements(Tablix.Namespace + "Textbox");

                    if (textboxes != null)
                    {
                        foreach (var textbox in textboxes)
                        {
                            TablixCellContent.Add(new Textbox(this, textbox, Row.Body.Tablix.DataSets));
                        }
                    }

                    // Process other types.
                }
            }
        }
    }

    public class Style
    {
        public string TextAlign { get; set; } = "Left";
        public Border Border { get; set; }
        public Border BorderBottom { get; set; }
        public string PaddingLeft { get; set; }
        public string PaddingRight { get; set; }
        public string PaddingTop { get; set; }
        public string PaddingBottom { get; set; }
        public string BackgroundColor { get; set; }
        public string VerticalAlign { get; set; }
        public string Top { get; set; }
        public string Left { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string ZIndex { get; set; }
        public string FontFamily { get; set; }
        public string FontWeight { get; set; }
        public string Color { get; set; }

        public Style()
        {

        }

        public Style(XElement style)
        {
            TextAlign = style?.Element(Tablix.Namespace + "TextAlign")?.Value;

            var border = style?.Element(Tablix.Namespace + "Border");
            var borderBottom = style?.Element(Tablix.Namespace + "BorderBottom");

            if (border != null)
            {
                Border = new Border
                {
                    Color = border.Element(Tablix.Namespace + "Color")?.Value,
                    Style = border.Element(Tablix.Namespace + "Style")?.Value,
                    Width = border.Element(Tablix.Namespace + "Width")?.Value
                };
            }

            if (borderBottom != null)
            {
                BorderBottom = new Border
                {
                    Color = borderBottom.Element(Tablix.Namespace + "Color")?.Value,
                    Style = borderBottom.Element(Tablix.Namespace + "Style")?.Value,
                    Width = borderBottom.Element(Tablix.Namespace + "Width")?.Value
                };
            }

            PaddingLeft = style?.Element(Tablix.Namespace + "PaddingLeft")?.Value;
            PaddingRight = style?.Element(Tablix.Namespace + "PaddingRight")?.Value;
            PaddingTop = style?.Element(Tablix.Namespace + "PaddingTop")?.Value;
            PaddingBottom = style?.Element(Tablix.Namespace + "PaddingBottom")?.Value;
            BackgroundColor = style?.Element(Tablix.Namespace + "BackgroundColor")?.Value;
            VerticalAlign = style?.Element(Tablix.Namespace + "VerticalAlign")?.Value;
            Top = style?.Element(Tablix.Namespace + "Top")?.Value;
            Left = style?.Element(Tablix.Namespace + "Left")?.Value;
            Height = style?.Element(Tablix.Namespace + "Height")?.Value;
            Width = style?.Element(Tablix.Namespace + "Width")?.Value;
            ZIndex = style?.Element(Tablix.Namespace + "ZIndex")?.Value;
            FontFamily = style?.Element(Tablix.Namespace + "FontFamily")?.Value;
            FontWeight = style?.Element(Tablix.Namespace + "FontWeight")?.Value;
            Color = style?.Element(Tablix.Namespace + "Color")?.Value;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append(!string.IsNullOrEmpty(TextAlign) ? $"text-align: {TextAlign.ToLower()};" : "");

            if (Border != null)
            {
                sb.Append(Border.Style == "None" ? "border: none;" : $"border: {Border.Width} solid {Border.Color};");
            }

            if (BorderBottom != null)
            {
                sb.Append(BorderBottom.Style == "None" ? "border: none;" : $"border-bottom: {BorderBottom.Width} solid {BorderBottom.Color};");
            }

            sb.Append(!string.IsNullOrEmpty(PaddingLeft) ? $"padding-left: {PaddingLeft};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingRight) ? $"padding-right: {PaddingRight};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingTop) ? $"padding-top: {PaddingTop};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingBottom) ? $"padding-bottom: {PaddingBottom};" : "");
            sb.Append(!string.IsNullOrEmpty(BackgroundColor) ? $"background-color: {BackgroundColor};" : "");
            sb.Append(!string.IsNullOrEmpty(VerticalAlign) ? $"vertical-align: {VerticalAlign};" : "");
            sb.Append(!string.IsNullOrEmpty(Top) ? $"top: {Top};" : "");
            sb.Append(!string.IsNullOrEmpty(Left) ? $"left: {Left};" : "");
            sb.Append(!string.IsNullOrEmpty(Height) ? $"height: {Height};" : "");
            sb.Append(!string.IsNullOrEmpty(Width) ? $"width: {Width};" : "");
            sb.Append(!string.IsNullOrEmpty(ZIndex) ? $"z-index: {ZIndex};" : "");
            sb.Append(!string.IsNullOrEmpty(FontFamily) ? $"font-family: {FontFamily};" : "");
            sb.Append(!string.IsNullOrEmpty(FontWeight) ? $"font-weight: {FontWeight};" : "");
            sb.Append(!string.IsNullOrEmpty(Color) ? $"color: {Color};" : "");

            //if (!string.IsNullOrEmpty(this.Top) || !string.IsNullOrEmpty(this.Left))
            //{
            //    sb.Append("position: absolute;");
            //}

            sb.Append("\"");

            return sb.ToString();
        }
    }

    public class Border
    {
        public string Style { get; set; } = "None";
        public string Color { get; set; } = "transparent";
        public string Width { get; set; } = "0px";
    }

    public class Textbox : ReportItem
    {
        public string Name { get; set; }
        public bool CanGrow { get; set; }
        public bool KeepTogether { get; set; }
        public List<Paragraph> Paragraphs { get; set; }
        public Style Style { get; set; }
        public TablixCell Cell { get; set; }
        public IEnumerable<DataSet> DataSets { get; set; }

        public Textbox(TablixCell cell, XElement textbox, IEnumerable<DataSet> dataSets)
            : this(textbox, dataSets)
        {
            Cell = cell;
        }

        public Textbox(XElement textbox, IEnumerable<DataSet> dataSets)
        {
            DataSets = dataSets;
            Paragraphs = new List<Paragraph>();

            Name = textbox.Attribute("Name")?.Value;
            CanGrow = textbox.Element(Tablix.Namespace + "CanGrow")?.Value == "true";
            KeepTogether = textbox.Element(Tablix.Namespace + "KeepTogether")?.Value == "true";

            var paragraphs = textbox.Elements(Tablix.Namespace + "Paragraphs").Elements(Tablix.Namespace + "Paragraph");
            var style = textbox.Element(Tablix.Namespace + "Style");

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Top")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Top").Value.TrimEnd('m'), out var top))
            {
                Top = top;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Left")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Left").Value.TrimEnd('m'), out var left))
            {
                Left = left;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Width")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Width").Value.TrimEnd('m'), out var width))
            {
                Width = width;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Height")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Height").Value.TrimEnd('m'), out var height))
            {
                Height = height;
            }

            Style = new Style(style);
            Style.Top = textbox.Element(Tablix.Namespace + "Top")?.Value;
            Style.Left = textbox.Element(Tablix.Namespace + "Left")?.Value;
            Style.Height = textbox.Element(Tablix.Namespace + "Height")?.Value;
            Style.Width = textbox.Element(Tablix.Namespace + "Width")?.Value;

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

            var textRuns = paragraph.Elements(Tablix.Namespace + "TextRuns").Elements(Tablix.Namespace + "TextRun");

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
            Value = textRun.Element(Tablix.Namespace + "Value")?.Value;
            Style = new Style(textRun.Element(Tablix.Namespace + "Style"));
            Format = textRun.Element(Tablix.Namespace + "Style").Element(Tablix.Namespace + "Format")?.Value;
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

    public class TablixExpression
    {
        public int Index { get; set; } = -1;
        public int EndIndex { get; set; }
        public TablixOperator Operator { get; set; }
        public string Field { get; set; }
        public Type ResolvedType { get; set; }
        public object Value { get; set; }
        public string DataSetName { get; set; }
    }

    public enum TablixOperator
    {
        None,
        Count,
        Field,
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
