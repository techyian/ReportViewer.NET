using System.Collections.Generic;
using System.Xml.Linq;
using ReportViewer.NET.DataObjects.ReportItems;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.DataObjects
{
    public class ReportRDL
    {
        public XDocument Xml { get; set; }
        public string Name { get; set; }
        public XNamespace Namespace { get; set; }
        public string ReportServerUrl { get; set; }
        public List<DataSource> DataSources { get; set; }
        public List<DataSet> DataSets { get; set; }
        public List<ReportParameter> ReportParameters { get; set; }
        public List<ReportItem> ReportHeaderItems { get; set; }
        public List<ReportItem> ReportBodyItems { get; set; }        
        public List<ReportItem> ReportFooterItems { get; set; }
        public List<EmbeddedImage> EmbeddedImages { get; set; }
        public string ReportWidth { get; set; }
        public List<ReportItem> HiddenItems { get; set; } = new List<ReportItem>();
        public List<TablixMember> HiddenTablixMembers { get; set; } = new List<TablixMember>();
        public List<ReportParameter> UserProvidedParameters { get; set; }
        public List<string> ToggleItemRequests { get; set; }
        public List<ReportRDL> CurrentRegisteredReports { get; set; }
        public List<ReportMetadata> Metadata { get; set; } = new List<ReportMetadata>();
        public ExpressionParser Parser { get; private set; }

        public ReportRDL()
        {
            this.Parser = new ExpressionParser(this);
        }
    }
}
