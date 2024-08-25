using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportViewerController
    {
        Task<IActionResult> ParameterViewer(string rdl, IEnumerable<ReportParameter> userProvidedParameters);        
        Task<IActionResult> ReportViewer(string rdl, IEnumerable<ReportParameter> userProvidedParameters, IEnumerable<string> requestedVisible);
    }
}
