using System;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class SubReport : ReportItem
    {
        private ReportRDL _subReportRdl;

        public SubReport(XElement element, ReportRDL report, ReportRDL subReportRdl, ReportItem parent) 
            : base(element, report, parent)
        {
            _subReportRdl = subReportRdl;
        }

        public ReportRDL GetSubReportRDL()
        {
            return _subReportRdl;
        }

        public override string Build(ReportItem parent)
        {
            throw new NotImplementedException();
        }
    }
}
