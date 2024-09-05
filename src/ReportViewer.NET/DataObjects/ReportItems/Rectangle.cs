using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Rectangle : ReportItem
    {
        public IEnumerable<ReportItem> ReportItems { get; set; }

        internal Rectangle(XElement element, ReportRDL report, IEnumerable<DataSet> datasets) : base(element, report)
        {
            var reportItems = element.Elements(report.Namespace + "ReportItems");

            this.ReportItems = ReportItem.ParseElements(reportItems, report, datasets, null);
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            sb.AppendLine($"<div {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\">");

            foreach (var reportItem in this.ReportItems)
            {
                sb.AppendLine(reportItem.Build(this));
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }
    }
}
