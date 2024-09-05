using System;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Line : ReportItem
    {
        public Line(XElement element, ReportRDL report) : base(element, report)
        {
        }

        public override string Build(ReportItem parent)
        {
            return $"<div {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\"></div>";
        }
    }
}
