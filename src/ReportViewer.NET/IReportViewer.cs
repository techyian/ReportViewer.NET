using Microsoft.AspNetCore.Html;
using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportViewer
    {
        ReportRDL RegisterRdl(string filepath);
        void RegisterDataSource(string name, string connectionString);
        Task<HtmlString> PublishReportParameters(string report, IEnumerable<ReportParameter> userProvidedParameters);
        Task<HtmlString> PublishReportOutput(string report, IEnumerable<ReportParameter> userProvidedParameters);
    }
}
