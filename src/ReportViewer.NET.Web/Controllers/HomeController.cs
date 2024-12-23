using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Web.Models;
using System.Diagnostics;

namespace ReportViewer.NET.Web.Controllers
{
    public class HomeController : Controller, IReportViewerController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IReportHandler _reportViewer;
        public HomeController(ILogger<HomeController> logger, IReportHandler reportViewer)
        {
            _logger = logger;
            _reportViewer = reportViewer;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateParameters([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
        {            
            _reportViewer.LoadReport(rdl, userProvidedParameters);
            var paramHtml = await _reportViewer.PublishReportParameters(rdl, userProvidedParameters.Parameters);

            return Ok(paramHtml);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport([FromQuery] string rdl, [FromBody] ReportParameters userProvidedParameters)
        {         
            _reportViewer.LoadReport(rdl, userProvidedParameters);
            var reportHtml = await _reportViewer.PublishReportOutput(rdl, userProvidedParameters);

            return Ok(reportHtml);
        }
    }
}
