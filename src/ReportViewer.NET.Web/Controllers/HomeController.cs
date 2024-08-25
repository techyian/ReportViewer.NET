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
        public async Task<IActionResult> ParameterViewer([FromQuery] string rdl, [FromBody] IEnumerable<ReportParameter> userProvidedParameters)
        {
            var paramHtml = await _reportViewer.PublishReportParameters(rdl, userProvidedParameters);

            return Ok(paramHtml);
        }

        [HttpPost]
        public async Task<IActionResult> ReportViewer([FromQuery] string rdl, [FromBody] IEnumerable<ReportParameter> userProvidedParameters, [FromBody] IEnumerable<string> requestedVisible)
        {
            var reportHtml = await _reportViewer.PublishReportOutput(rdl, userProvidedParameters, requestedVisible);

            return Ok(reportHtml);
        }
    }
}
