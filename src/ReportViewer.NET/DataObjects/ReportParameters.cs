using System.Collections.Generic;

namespace ReportViewer.NET.DataObjects
{
    public class ReportParameters
    {
        public List<ReportParameter> Parameters { get; set; }
        public List<string> ToggleItemRequests { get; set; }
    }
}
