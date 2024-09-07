using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Rectangle : ReportItem
    {
        public IEnumerable<ReportItem> ReportItems { get; set; }

        internal Rectangle(XElement element, ReportRDL report, IEnumerable<DataSet> datasets, ReportItem parent) : base(element, report, parent)
        {
            var reportItems = element.Elements(report.Namespace + "ReportItems");

            // Using 'this' as parent may cause issues with aggregated grouping totals. Bear this in mind if issues occur.
            this.ReportItems = ReportItem.ParseElements(reportItems, report, datasets, null, this);
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
