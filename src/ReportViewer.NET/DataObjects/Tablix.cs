using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects
{
    public abstract class ReportItem
    {
        public abstract string Build();
    }

    public class Tablix : ReportItem
    {
        public string Name { get; set; }
        public string DataSetName { get; set; }
        public DataSetReference DataSetReference { get; set; }        
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }
        public Style Style { get; set; }
        public TablixBody TablixBody { get; set; }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<table {this.Style?.Build()} class=\"table reportviewer-table\">");
            sb.AppendLine(this.TablixBody?.Build());
            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }

    public class TablixBody
    {
        public List<TablixColumn> TablixColumns { get; set; }
        public List<TablixRow> TablixRows { get; set; }

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
            foreach (var row in this.TablixRows)
            {
                sb.AppendLine("<tr>");

                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</tbody>");
                        
            return sb.ToString();
        }
    }

    public class TablixColumn
    {
        public string Width { get; set; }

        public string Build()
        {
            return @"<td width=""" + this.Width + @"""></td>";
        }
    }

    public class TablixRow
    {
        public string Height { get;set; }
        public List<TablixCell> TablixCells { get; set; }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<td height=""" + this.Height + @"""></td>");

            if (this.TablixCells != null)
            {
                foreach (var cell in this.TablixCells)
                {
                    if (cell.TablixCellContent != null)
                    {
                        foreach (var content in cell.TablixCellContent)
                        {
                            sb.AppendLine(content.Build());
                        }
                    }                    
                }
            }

            return sb.ToString();
        }
    }

    public class TablixCell
    {                
        public List<TablixCellContent> TablixCellContent { get; set; }
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

        public Style(XElement style, XNamespace ns)
        {
            this.TextAlign = style?.Element(ns + "TextAlign")?.Value;

            var border = style?.Element(ns + "Border");
            var borderBottom = style?.Element(ns + "BorderBottom");

            if (border != null)
            {
                this.Border = new Border
                {
                    Color = border.Element(ns + "Color")?.Value,
                    Style = border.Element(ns + "Style")?.Value,
                    Width = border.Element(ns + "Width")?.Value
                };
            }

            if (borderBottom != null)
            {
                this.BorderBottom = new Border
                {
                    Color = borderBottom.Element(ns + "Color")?.Value,
                    Style = borderBottom.Element(ns + "Style")?.Value,
                    Width = borderBottom.Element(ns + "Width")?.Value
                };
            }

            this.PaddingLeft = style?.Element(ns + "PaddingLeft")?.Value;
            this.PaddingRight = style?.Element(ns + "PaddingRight")?.Value;
            this.PaddingTop = style?.Element(ns + "PaddingTop")?.Value;
            this.PaddingBottom = style?.Element(ns + "PaddingBottom")?.Value;
            this.BackgroundColor = style?.Element(ns + "BackgroundColor")?.Value;
            this.VerticalAlign = style?.Element(ns + "VerticalAlign")?.Value;
            this.Top = style?.Element(ns + "Top")?.Value;
            this.Left = style?.Element(ns + "Left")?.Value;
            this.Height = style?.Element(ns + "Height")?.Value;
            this.Width = style?.Element(ns + "Width")?.Value;
            this.ZIndex = style?.Element(ns + "ZIndex")?.Value;
            this.FontFamily = style?.Element(ns + "FontFamily")?.Value;
            this.FontWeight = style?.Element(ns + "FontWeight")?.Value;
            this.Color = style?.Element(ns + "Color")?.Value;
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

            if (!string.IsNullOrEmpty(this.Top) || !string.IsNullOrEmpty(this.Left))
            {
                sb.Append("position: absolute;");
            }

            sb.Append("\"");

            return sb.ToString();
        }
    }

    public class Paragraph
    {
        public List<TextRun> TextRuns { get; set; }
        public Style Style { get; set; }

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
        public string Value { get; set; }
        public Style Style { get; set; }

        public string Build()
        {
            if (this.Value.StartsWith('='))
            {
                return $"<span {this.Style?.Build()}>{{{{{this.Value}}}}}</span>";
            }
            
            return $"<span {this.Style?.Build()}>{this.Value}</span>";
        }
    }

    public class Border
    {
        public string Style { get; set; } = "None";
        public string Color { get; set; } = "transparent";
        public string Width { get; set; } = "0px";
    }

    public abstract class TablixCellContent : ReportItem
    {        
    }

    public class Textbox : TablixCellContent
    {
        public string Name { get; set; }
        public bool CanGrow { get; set; }
        public bool KeepTogether { get; set; }
        public List<Paragraph> Paragraphs { get; set; }        
        public Style Style { get; set; }

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
