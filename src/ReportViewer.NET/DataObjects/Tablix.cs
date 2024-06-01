using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects
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
            this.DataSets = datasets;
            this.TablixBodyObj = new TablixBody(this, tablix.Element(Namespace + "TablixBody"));

            this.DataSetName = tablix.Element(Namespace + "DataSetName")?.Value;
            this.Hidden = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "Hidden")?.Value == "true";
            this.ToggleItem = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "ToggleItem")?.Value;

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Top")?.Value) && double.TryParse(tablix.Element(Namespace + "Top").Value.TrimEnd('m'), out var top))
            {
                this.Top = top;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Left")?.Value) && double.TryParse(tablix.Element(Namespace + "Left").Value.TrimEnd('m'), out var left))
            {
                this.Left = left;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Width")?.Value) && double.TryParse(tablix.Element(Namespace + "Width").Value.TrimEnd('m'), out var width))
            {
                this.Width = width;
            }

            if (!string.IsNullOrEmpty(tablix.Element(Namespace + "Height")?.Value) && double.TryParse(tablix.Element(Namespace + "Height").Value.TrimEnd('m'), out var height))
            {
                this.Height = height;
            }

            this.Style = new Style(tablix.Element(Namespace + "Style"));
            this.Style.Top = tablix.Element(Namespace + "Top")?.Value;
            this.Style.Left = tablix.Element(Namespace + "Left")?.Value;
            this.Style.Height = tablix.Element(Namespace + "Height")?.Value;
            this.Style.Width = tablix.Element(Namespace + "Width")?.Value;

            if (!string.IsNullOrEmpty(this.DataSetName))
            {
                this.DataSetReference = new DataSetReference()
                {
                    DataSetName = this.DataSetName
                };

                this.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == this.DataSetReference.DataSetName);
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<table {this.Style?.Build()} class=\"table reportviewer-table\">");
            sb.AppendLine(this.TablixBodyObj?.Build());
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
            this.Tablix = tablix;
            this.TablixColumns = new List<TablixColumn>();
            this.TablixRows = new List<TablixRow>();

            var columns = tablixBody.Elements(Tablix.Namespace + "TablixColumns").Elements(Tablix.Namespace + "TablixColumn");
            var rows = tablixBody.Elements(Tablix.Namespace + "TablixRows").Elements(Tablix.Namespace + "TablixRow");

            if (columns != null)
            {
                foreach (var c in columns)
                {
                    this.TablixColumns.Add(new TablixColumn(this, c));
                }
            }

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    this.TablixRows.Add(new TablixRow(this, r));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            foreach (var column in this.TablixColumns)
            {
                sb.AppendLine(column.Build());
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");

            for (var i = 0; i < this.TablixRows.Count; i++)
            {
                var row = this.TablixRows[i];

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
            this.Body = body;
            this.Width = column.Element(Tablix.Namespace + "Width")?.Value;
        }

        public string Build()
        {
            return @"<td width=""" + this.Width + @"""></td>";
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
            this.Body = body;
            this.Height = row.Element(Tablix.Namespace + "Height")?.Value;
            this.TablixCells = new List<TablixCell>();

            var cells = row.Elements(Tablix.Namespace + "TablixCells").Elements(Tablix.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    this.TablixCells.Add(new TablixCell(this, c));

                    if (!this.ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        this.ContainsRepeatExpression = !LayoutProvider.CountRegex.IsMatch(c.Value) && LayoutProvider.FieldRegex.IsMatch(c.Value);
                    }
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();
                        
            sb.AppendLine(@"<tr height=""" + this.Height + @""">");
                        
            if (this.TablixCells != null)
            {
                foreach (var cell in this.TablixCells)
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
            this.Row = row;
            this.TablixCellContent = new List<ReportItem>();

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
                            this.TablixCellContent.Add(new Textbox(this, textbox, this.Row.Body.Tablix.DataSets));
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
            this.TextAlign = style?.Element(Tablix.Namespace + "TextAlign")?.Value;

            var border = style?.Element(Tablix.Namespace + "Border");
            var borderBottom = style?.Element(Tablix.Namespace + "BorderBottom");

            if (border != null)
            {
                this.Border = new Border
                {
                    Color = border.Element(Tablix.Namespace + "Color")?.Value,
                    Style = border.Element(Tablix.Namespace + "Style")?.Value,
                    Width = border.Element(Tablix.Namespace + "Width")?.Value
                };
            }

            if (borderBottom != null)
            {
                this.BorderBottom = new Border
                {
                    Color = borderBottom.Element(Tablix.Namespace + "Color")?.Value,
                    Style = borderBottom.Element(Tablix.Namespace + "Style")?.Value,
                    Width = borderBottom.Element(Tablix.Namespace + "Width")?.Value
                };
            }

            this.PaddingLeft = style?.Element(Tablix.Namespace + "PaddingLeft")?.Value;
            this.PaddingRight = style?.Element(Tablix.Namespace + "PaddingRight")?.Value;
            this.PaddingTop = style?.Element(Tablix.Namespace + "PaddingTop")?.Value;
            this.PaddingBottom = style?.Element(Tablix.Namespace + "PaddingBottom")?.Value;
            this.BackgroundColor = style?.Element(Tablix.Namespace + "BackgroundColor")?.Value;
            this.VerticalAlign = style?.Element(Tablix.Namespace + "VerticalAlign")?.Value;
            this.Top = style?.Element(Tablix.Namespace + "Top")?.Value;
            this.Left = style?.Element(Tablix.Namespace + "Left")?.Value;
            this.Height = style?.Element(Tablix.Namespace + "Height")?.Value;
            this.Width = style?.Element(Tablix.Namespace + "Width")?.Value;
            this.ZIndex = style?.Element(Tablix.Namespace + "ZIndex")?.Value;
            this.FontFamily = style?.Element(Tablix.Namespace + "FontFamily")?.Value;
            this.FontWeight = style?.Element(Tablix.Namespace + "FontWeight")?.Value;
            this.Color = style?.Element(Tablix.Namespace + "Color")?.Value;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append(!string.IsNullOrEmpty(this.TextAlign) ? $"text-align: {this.TextAlign.ToLower()};" : "");

            if (this.Border != null)
            {
                sb.Append(this.Border.Style == "None" ? "border: none;" : $"border: {this.Border.Width} solid {this.Border.Color};");
            }

            if (this.BorderBottom != null)
            {
                sb.Append(this.BorderBottom.Style == "None" ? "border: none;" : $"border-bottom: {this.BorderBottom.Width} solid {this.BorderBottom.Color};");
            }

            sb.Append(!string.IsNullOrEmpty(this.PaddingLeft) ? $"padding-left: {this.PaddingLeft};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingRight) ? $"padding-right: {this.PaddingRight};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingTop) ? $"padding-top: {this.PaddingTop};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingBottom) ? $"padding-bottom: {this.PaddingBottom};" : "");
            sb.Append(!string.IsNullOrEmpty(this.BackgroundColor) ? $"background-color: {this.BackgroundColor};" : "");
            sb.Append(!string.IsNullOrEmpty(this.VerticalAlign) ? $"vertical-align: {this.VerticalAlign};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Top) ? $"top: {this.Top};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Left) ? $"left: {this.Left};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Height) ? $"height: {this.Height};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Width) ? $"width: {this.Width};" : "");
            sb.Append(!string.IsNullOrEmpty(this.ZIndex) ? $"z-index: {this.ZIndex};" : "");
            sb.Append(!string.IsNullOrEmpty(this.FontFamily) ? $"font-family: {this.FontFamily};" : "");
            sb.Append(!string.IsNullOrEmpty(this.FontWeight) ? $"font-weight: {this.FontWeight};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Color) ? $"color: {this.Color};" : "");

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
            this.Cell = cell;            
        }

        public Textbox(XElement textbox, IEnumerable<DataSet> dataSets)
        {
            this.DataSets = dataSets;
            this.Paragraphs = new List<Paragraph>();
            
            this.Name = textbox.Attribute("Name")?.Value;
            this.CanGrow = textbox.Element(Tablix.Namespace + "CanGrow")?.Value == "true";
            this.KeepTogether = textbox.Element(Tablix.Namespace + "KeepTogether")?.Value == "true";

            var paragraphs = textbox.Elements(Tablix.Namespace + "Paragraphs").Elements(Tablix.Namespace + "Paragraph");
            var style = textbox.Element(Tablix.Namespace + "Style");

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Top")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Top").Value.TrimEnd('m'), out var top))
            {
                this.Top = top;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Left")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Left").Value.TrimEnd('m'), out var left))
            {
                this.Left = left;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Width")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Width").Value.TrimEnd('m'), out var width))
            {
                this.Width = width;
            }

            if (!string.IsNullOrEmpty(textbox.Element(Tablix.Namespace + "Height")?.Value) && double.TryParse(textbox.Element(Tablix.Namespace + "Height").Value.TrimEnd('m'), out var height))
            {
                this.Height = height;
            }

            this.Style = new Style(style);
            this.Style.Top = textbox.Element(Tablix.Namespace + "Top")?.Value;
            this.Style.Left = textbox.Element(Tablix.Namespace + "Left")?.Value;
            this.Style.Height = textbox.Element(Tablix.Namespace + "Height")?.Value;
            this.Style.Width = textbox.Element(Tablix.Namespace + "Width")?.Value;

            if (paragraphs != null)
            {
                foreach (var p in paragraphs)
                {
                    this.Paragraphs.Add(new Paragraph(this, p));                                        
                }
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<div {this.Style?.Build()}>");
            
            if (this.Paragraphs != null)
            {
                foreach (var p in this.Paragraphs)
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
            this.Textbox = textbox;
            this.TextRuns = new List<TextRun>();

            var textRuns = paragraph.Elements(Tablix.Namespace + "TextRuns").Elements(Tablix.Namespace + "TextRun");

            if (textRuns != null)
            {                
                foreach (var tr in textRuns)
                {                    
                    this.TextRuns.Add(new TextRun(this, tr));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<p {this.Style?.Build()}>");

            if (this.TextRuns != null)
            {
                foreach (var tr in this.TextRuns)
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
             
        public TextRun(Paragraph paragraph, XElement textRun)
        {
            this.Paragraph = paragraph;
            this.Value = textRun.Element(Tablix.Namespace + "Value")?.Value;
            this.Style = new Style(textRun.Element(Tablix.Namespace + "Style"));
        }

        public string Build()
        {
            if (this.Value.StartsWith('='))
            {
                var cell = this.Paragraph.Textbox.Cell;

                if (cell != null)
                {
                    // We've come from a tablix cell.
                    if (cell.Row.Values != null)
                    {
                        var parsedValue = this.ParseTablixExpressionString(this.Value, cell.Row.Body.Tablix.DataSetReference?.DataSet?.DataSetResults, cell.Row.Values, null);
                        return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                    }
                }
                else
                {
                    // We've come from a standalone textbox. Try to find dataset for this field.
                    var parsedValue = this.ParseTablixExpressionString(this.Value, null, null, this.Paragraph.Textbox.DataSets);

                    return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                }
            }

            return $"<span {this.Style?.Build()}>{this.Value}</span>";
        }

        private string ParseTablixExpressionString(
            string tablixText, 
            IEnumerable<dynamic> dataSetResults,
            dynamic values, 
            IEnumerable<DataObjects.DataSet> dataSets
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

                    this.ParseCountExpression(currentString, currentExpression, dataSetResults, values, dataSets);

                    proposedString = (endIndx == currentString.Length) ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
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

                    this.ParseFieldExpression(currentString, currentExpression, dataSetResults, values, dataSets);

                    proposedString = (endIndx == currentString.Length) ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
                }

                if (currentExpression.Operator == TablixOperator.None)
                {
                    break;
                }

                expressions.Add(currentExpression);
                currentString = proposedString;
            }

            return this.ParseTablixExpression(expressions);
        }

        private string ParseTablixExpression(IEnumerable<TablixExpression> expressions)
        {
            TablixOperator lastOperator;

            if (expressions.Count() > 0)
            {
                var final = expressions.Aggregate((prev, next) =>
                {
                    var newExpr = new TablixExpression();

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

                if (final.Value == null)
                {
                    return string.Empty;
                }

                return final.Value.ToString();
            }

            return string.Empty;
        }

        private void ParseCountExpression(
            string currentString, 
            TablixExpression expression, 
            IEnumerable<dynamic> dataSetResults,
            dynamic values, 
            IEnumerable<DataObjects.DataSet> dataSets
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
                                                   
                expression.Value = this.ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetResults, values, dataSets);
            }
        }

        private void ParseFieldExpression(
            string currentString, 
            TablixExpression expression, 
            IEnumerable<dynamic> dataSetResults,
            dynamic values, 
            IEnumerable<DataObjects.DataSet> dataSets
        )
        {
            var fieldsIdx = currentString.IndexOf("Fields!");
            var fieldEnd = currentString.IndexOf(".", fieldsIdx);
            var fieldName = currentString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

            expression.Index = fieldsIdx;
            expression.Field = fieldName;
            expression.Value = this.ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetResults, values, dataSets);
        }

        private dynamic ExtractExpressionValue(
            string dataSetName, 
            string fieldName, 
            TablixOperator op, 
            IEnumerable<dynamic> dataSetResults, 
            dynamic values, 
            IEnumerable<DataObjects.DataSet> dataSets
        )
        {
            if (dataSetResults != null)
            {                
                switch (op)
                {
                    case TablixOperator.Count:
                        return dataSetResults.Count();
                    case TablixOperator.Field:
                        var expando = (IDictionary<string, object>)values;
                        if (expando.ContainsKey(fieldName))
                        {
                            return expando[fieldName];
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
                                return dataSet.DataSetResults.Count();
                            case TablixOperator.Field:
                                foreach (IDictionary<string, object> expando in dataSet.DataSetResults)
                                {
                                    if (expando.ContainsKey(fieldName))
                                    {
                                        return expando[fieldName];
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return null;
        }
    }

    public class TablixExpression
    {
        public int Index { get; set; } = -1;
        public int EndIndex { get; set; }
        public TablixOperator Operator { get; set; }
        public string Field { get; set; }
        public dynamic Value { get; set; }
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
