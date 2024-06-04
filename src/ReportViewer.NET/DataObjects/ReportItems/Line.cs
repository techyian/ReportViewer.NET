using System;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Line : ReportItem
    {
        public Line(XElement element) : base(element)
        {
        }

        public override string Build()
        {
            return $"<div {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\"></div>";
        }
    }
}
