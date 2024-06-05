using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Linq;
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

        private const string _expandSvg = @"
            <svg fill=""#000000"" version=""1.1"" id=""Capa_1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" 
	             width=""10px"" height=""10px"" viewBox=""0 0 45.402 45.402""
	             xml:space=""preserve"">
            <g>
	            <path d=""M41.267,18.557H26.832V4.134C26.832,1.851,24.99,0,22.707,0c-2.283,0-4.124,1.851-4.124,4.135v14.432H4.141
		            c-2.283,0-4.139,1.851-4.138,4.135c-0.001,1.141,0.46,2.187,1.207,2.934c0.748,0.749,1.78,1.222,2.92,1.222h14.453V41.27
		            c0,1.142,0.453,2.176,1.201,2.922c0.748,0.748,1.777,1.211,2.919,1.211c2.282,0,4.129-1.851,4.129-4.133V26.857h14.435
		            c2.283,0,4.134-1.867,4.133-4.15C45.399,20.425,43.548,18.557,41.267,18.557z""/>
            </g>
            </svg>
        ";

        public Textbox(TablixCell cell, XElement textbox, IEnumerable<DataSet> dataSets, ReportRDL report)
            : this(textbox, dataSets, report)
        {
            Cell = cell;
        }

        public Textbox(XElement textbox, IEnumerable<DataSet> dataSets, ReportRDL report)
            : base(textbox, report)
        {
            this.DataSets = dataSets;
            this.Paragraphs = new List<Paragraph>();

            this.Name = textbox.Attribute("Name")?.Value;
            this.CanGrow = textbox.Element(report.Namespace + "CanGrow")?.Value == "true";
            this.KeepTogether = textbox.Element(report.Namespace + "KeepTogether")?.Value == "true";

            var paragraphs = textbox.Elements(report.Namespace + "Paragraphs").Elements(report.Namespace + "Paragraph");
            
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
            
            sb.AppendLine($"<div {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\">");

            if (this.DoesToggle)
            {
                sb.AppendLine($"<button class=\"reportitem-expand\" data-toggler-name=\"{this.Name}\" data-toggler=\"true\">{_expandSvg}</button>");
            }

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
            this.Style = new Style(paragraph.Element(this.Textbox.Report.Namespace + "Style"), this.Textbox.Report);
            this.TextRuns = new List<TextRun>();

            var textRuns = paragraph.Elements(this.Textbox.Report.Namespace + "TextRuns").Elements(this.Textbox.Report.Namespace + "TextRun");

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
            this.Value = textRun.Element(this.Paragraph.Textbox.Report.Namespace + "Value")?.Value;
            this.Style = new Style(textRun.Element(this.Paragraph.Textbox.Report.Namespace + "Style"), this.Paragraph.Textbox.Report);
            this.Format = textRun.Element(this.Paragraph.Textbox.Report.Namespace + "Style").Element(this.Paragraph.Textbox.Report.Namespace + "Format")?.Value;
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
                    if (cell.Row != null)
                    {
                        var dataSetResults = cell.Row.GroupedResults?.Select(r => r).ToList() ?? cell.Row.Body.Tablix.DataSetReference?.DataSet?.DataSetResults;
                        var parsedValue = this.Parser.ParseTablixExpressionString(this.Value, dataSetResults, cell.Row.Values, null, this.Format);

                        return $"<span {this.Style?.Build()}>{parsedValue}</span>";
                    }
                    else if (cell.Header != null)
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
