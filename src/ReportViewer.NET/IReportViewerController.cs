using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportViewerController
    {
        Task<IActionResult> ParameterViewer(IEnumerable<ReportParameter> userProvidedParameters);        
        Task<IActionResult> ReportViewer(IEnumerable<ReportParameter> userProvidedParameters);
    }
}
