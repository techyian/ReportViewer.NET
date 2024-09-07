using System;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Line : ReportItem
    {
        public Line(XElement element, ReportRDL report, ReportItem parent) : base(element, report, parent)
        {
        }

        public override string Build(ReportItem parent)
        {
            return $"<div {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\"></div>";
        }
    }
}
