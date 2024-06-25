using System;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class SubReport : ReportItem
    {
        private ReportRDL _subReportRdl;

        public SubReport(XElement element, ReportRDL report, ReportRDL subReportRdl) : base(element, report)
        {
            _subReportRdl = subReportRdl;
        }

        public ReportRDL GetSubReportRDL()
        {
            return _subReportRdl;
        }

        public override string Build()
        {
            throw new NotImplementedException();
        }
    }
}
