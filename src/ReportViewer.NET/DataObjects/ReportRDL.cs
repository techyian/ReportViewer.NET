using System.Collections.Generic;
using System.Xml.Linq;
using ReportViewer.NET.DataObjects.ReportItems;

namespace ReportViewer.NET.DataObjects
{
    public class ReportRDL
    {
        public string Name { get; set; }
        public XNamespace Namespace { get; set; }
        public string ReportServerUrl { get; set; }
        public List<DataSource> DataSources { get; set; }
        public List<DataSet> DataSets { get; set; }
        public List<ReportParameter> ReportParameters { get; set; }
        public List<ReportItem> ReportBodyItems { get; set; }
        public List<ReportItem> ReportFooterItems { get; set; }
        public List<EmbeddedImage> EmbeddedImages { get; set; }
        public string ReportWidth { get; set; }
        public List<ReportItem> HiddenItems { get; set; } = new List<ReportItem>();
        public List<ReportParameter> UserProvidedParameters { get; set; }
        public List<string> RequestedVisible { get; set; }
    }
}
