using System.Collections.Generic;
using System.Text;

namespace ReportViewer.NET.DataObjects
{
    internal abstract class ReportItem
    {
        internal abstract string Build();
    }

    internal class Tablix : ReportItem
    {
        internal string Name { get; set; }
        internal string DataSetName { get; set; }
        internal string Top { get; set; }
        internal string Left { get; set; }
        internal string Height { get; set; }
        internal string Width { get; set; }
        internal bool Hidden { get; set; }
        internal string ToggleItem { get; set; }
        internal Style Style { get; set; }
        internal TablixBody TablixBody { get; set; }

        internal override string Build()
        {
            return this.TablixBody?.Build() ?? string.Empty;
        }
    }

    internal class TablixBody
    {
        internal List<TablixColumn> TablixColumns { get; set; }
        internal List<TablixRow> TablixRows { get; set; }

        internal string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<table class=\"table reportviewer-table\">");
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

            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }

    internal class TablixColumn
    {
        internal string Width { get; set; }

        internal string Build()
        {
            return @"<td width=""" + this.Width + @"""></td>";
        }
    }

    internal class TablixRow
    {
        internal string Height { get;set; }
        internal List<TablixCell> TablixCells { get; set; }

        internal string Build()
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

    internal class TablixCell
    {                
        internal List<TablixCellContent> TablixCellContent { get; set; }
    }

    internal class Style
    {
        internal string TextAlign { get; set; } = "Left";
        internal Border Border { get; set; }
        internal Border BorderBottom { get; set; }
        internal string PaddingLeft { get; set; }
        internal string PaddingRight { get; set; }
        internal string PaddingTop { get; set; }
        internal string PaddingBottom { get; set; }
        internal string BackgroundColor { get; set; }
        internal string VerticalAlign { get; set; }
        internal string Top { get; set; }
        internal string Left { get; set; }
        internal string Height { get; set; }
        internal string Width { get; set; }
        internal string ZIndex { get; set; }

        internal string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append($"text-align: {this.TextAlign.ToLower()};");

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

            sb.Append("\"");

            return sb.ToString();
        }
    }

    internal class Paragraph
    {
        internal List<TextRun> TextRuns { get; set; }
        internal Style Style { get; set; }

        internal string Build()
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

    internal class TextRun
    {
        internal string Value { get; set; }
        internal Style Style { get; set; }

        internal string Build()
        {
            return $"<span {this.Style?.Build()}>{this.Value}</span>";
        }
    }

    internal class Border
    {
        internal string Style { get; set; } = "None";
        internal string Color { get; set; } = "transparent";
        internal string Width { get; set; } = "0px";
    }

    internal abstract class TablixCellContent : ReportItem
    {        
    }

    internal class Textbox : TablixCellContent
    {
        internal string Name { get; set; }
        internal bool CanGrow { get; set; }
        internal bool KeepTogether { get; set; }
        internal List<Paragraph> Paragraphs { get; set; }        
        internal Style Style { get; set; }

        internal override string Build()
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
}
