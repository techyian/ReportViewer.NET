using System.Collections.Generic;
using System.Text;
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
            this.Style = new Style(paragraph.Element(ReportItem.Namespace + "Style"));
            this.TextRuns = new List<TextRun>();

            var textRuns = paragraph.Elements(ReportItem.Namespace + "TextRuns").Elements(ReportItem.Namespace + "TextRun");

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
        public string Value { get; set; }
        public Style Style { get; set; }
        public bool ContainsDataSetExpression { get; set; }
        public Paragraph Paragraph { get; set; }
        public string Format { get; set; }

        internal ExpressionParser Parser { get; set; }

        public TextRun(Paragraph paragraph, XElement textRun)
        {
            this.Paragraph = paragraph;
            this.Value = textRun.Element(ReportItem.Namespace + "Value")?.Value;
            this.Style = new Style(textRun.Element(ReportItem.Namespace + "Style"));
            this.Format = textRun.Element(ReportItem.Namespace + "Style").Element(ReportItem.Namespace + "Format")?.Value;
            this.Parser = new ExpressionParser();
        }

        public string Build()
        {
            if (this.Value.StartsWith('='))
            {
                TablixCell cell = this.Paragraph.Textbox.Cell;

                if (cell != null)
                {
                    // We've come from a tablix cell.
                    if (cell.Row?.Values != null)
                    {
                        var parsedValue = this.Parser.ParseTablixExpressionString(this.Value, cell.Row.Body.Tablix.DataSetReference?.DataSet?.DataSetResults, cell.Row.Values, null, this.Format);

                        return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                    }
                    else if (cell.Header?.TablixMember?.Values != null)
                    {
                        var parsedValue = this.Parser.ParseTablixExpressionString(this.Value, cell.Header.TablixMember.TablixHierarchy.Tablix.DataSetReference?.DataSet?.DataSetResults, cell.Header.TablixMember.Values, null, this.Format);

                        return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                    }
                }
                else
                {
                    // We've come from a standalone textbox. Try to find dataset for this field.
                    var parsedValue = this.Parser.ParseTablixExpressionString(this.Value, null, null, this.Paragraph.Textbox.DataSets, Format);

                    return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                }
            }

            return $"<span {this.Style?.Build()}>{this.Value}</span>";
        }                
    }
}
