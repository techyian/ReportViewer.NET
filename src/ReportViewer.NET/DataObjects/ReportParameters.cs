using System.Collections.Generic;

namespace ReportViewer.NET.DataObjects
{
    public class ReportParameters
    {
        public List<ReportParameter> Parameters { get; set; }
        public List<string> ToggleItemRequests { get; set; }
        public List<ReportMetadata> Metadata { get; set; }
    }

    public class ReportMetadata
    {
        public string Key { get; set; }
        public string ObjectName { get; set; }
        public string Value { get; set; }

        public static readonly string TablixPageKey = "key_tablixpage";
    }
}
