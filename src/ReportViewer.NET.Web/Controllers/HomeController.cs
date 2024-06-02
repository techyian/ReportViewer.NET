using Microsoft.AspNetCore.Mvc;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Web.Models;
using System.Diagnostics;

namespace ReportViewer.NET.Web.Controllers
{
    public class HomeController : Controller, IReportViewerController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IReportViewer _reportViewer;
        public HomeController(ILogger<HomeController> logger, IReportViewer reportViewer)
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
        public async Task<IActionResult> ParameterViewer([FromBody] IEnumerable<ReportParameter> userProvidedParameters)
        {
            var paramHtml = await _reportViewer.PublishReportParameters("TMSAttendanceOverviewReport.rdl", userProvidedParameters);

            return Ok(paramHtml);
        }

        [HttpPost]
        public async Task<IActionResult> ReportViewer([FromBody] IEnumerable<ReportParameter> userProvidedParameters)
        {
            var reportHtml = await _reportViewer.PublishReportOutput("TMSAttendanceOverviewReport.rdl", userProvidedParameters);

            return Ok(reportHtml);
        }
    }
}
