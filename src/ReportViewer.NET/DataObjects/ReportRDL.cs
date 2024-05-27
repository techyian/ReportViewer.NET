using System;
using System.Collections.Generic;
using System.Text;

namespace ReportViewer.NET.DataObjects
{
    public class ReportRDL
    {
        public string Name { get; set; }
        public string ReportServerUrl { get; set; }
        public List<DataSource> DataSources { get; set; }
        public List<DataSet> DataSets { get; set; }
        public List<ReportParameter> ReportParameters { get; set; }
        internal List<ReportItem> ReportItems { get; set; }  
        
        internal string Render()
        {
            var sb = new StringBuilder();

            if (this.ReportItems != null)
            {
                foreach (var item in this.ReportItems)
                {
                    sb.Append(item.Build());
                }
            }

            return sb.ToString();
        }
    }
}
