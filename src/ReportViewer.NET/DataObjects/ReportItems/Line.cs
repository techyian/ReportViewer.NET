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
            if (!this.Hidden || (this.Hidden && this.Report.ToggleItemRequests.Contains(this.ToggleItem)))
            {
                this.Hidden = false;
                this.Style.Hidden = false;
                this.Style.Position = "absolute";

                return $"<div class=\"reportviewer-line\" {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\"></div>";
            }

            return string.Empty;
        }
    }
}
