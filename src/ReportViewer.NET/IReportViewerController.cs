using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportViewerController
    {
        Task<IActionResult> ParameterViewer(string rdl, ReportParameters userProvidedParameters);        
        Task<IActionResult> ReportViewer(string rdl, ReportParameters userProvidedParameters);
    }
}
