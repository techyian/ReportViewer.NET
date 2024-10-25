using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System.Globalization;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class TodayParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public TodayParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Today_Returns_DateTime()
        {
            // Arrange
            var expr = "=Today()";

            // Act
            var now = DateTime.Now;
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsDateTime();

            // Assert
            Assert.AreEqual(new DateTime(now.Year, now.Month, now.Day), result);            
        }

        [TestMethod]
        public void Nested_Today_Returns_String()
        {
            // Arrange
            var expr = "=\"Tomorrow is \" & DateAdd(\"d\",1,Today())";
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Act
            var now = DateTime.Now;
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual($"Tomorrow is {now.AddDays(1).ToString("dd/MM/yyyy 00:00:00")}", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }

        [TestMethod]
        public void Nested_Today_Returns_String_Formatted()
        {
            // Arrange
            var expr = "=\"Tomorrow is \" & FormatDateTime(DateAdd(\"d\",1,Today()), DateFormat.ShortDate)";
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Act
            var now = DateTime.Now;
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual($"Tomorrow is {now.AddDays(1).ToString("d")}", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }
    }
}
