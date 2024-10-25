using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System.Globalization;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class TimeValueParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public TimeValueParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void TimeValue_Returns_Time_From_01_01_0001_Provided_FullDate()
        {
            // Arrange
            var expr = "=TimeValue(\"January 15, 2010 14:15:30\")";
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Act
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
            Assert.AreEqual(new DateTime(1, 1, 1, 14, 15, 30), result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }

        [TestMethod]
        public void TimeValue_Returns_Time_From_01_01_0001_Provided_Time_Only()
        {
            // Arrange
            var expr = "=TimeValue(\"14:15:30\")";
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            // Act
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
            Assert.AreEqual(new DateTime(1, 1, 1, 14, 15, 30), result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }
    }
}
