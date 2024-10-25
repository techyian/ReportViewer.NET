using Microsoft.AspNetCore.Html;
using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportHandler
    {
        void RegisterRdlFromFile(string rdlName, string filePath);
        void RegisterRdlFromString(string rdlName, string rdlXml);
        ReportRDL LoadReport(string rdlName, ReportParameters userProvidedParameters);
        void RegisterDataSource(string name, string connectionString, string datasourceReference = null);
        Task<HtmlString> PublishReportParameters(string report, IEnumerable<ReportParameter> userProvidedParameters);
        Task<HtmlString> PublishReportOutput(string report, ReportParameters parameters);
    }
}
