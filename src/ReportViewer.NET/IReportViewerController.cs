using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using System.Threading.Tasks;

namespace ReportViewer.NET
{
    public interface IReportViewerController
    {
        Task<IActionResult> GenerateParameters(string rdl, ReportParameters userProvidedParameters);        
        Task<IActionResult> GenerateReport(string rdl, ReportParameters userProvidedParameters);
    }
}
