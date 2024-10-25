using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class TimeOfDayParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public TimeOfDayParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void TimeOfDay_Returns_Date_With_Default_Format()
        {
            // Limiting to hour and minute to prevent test failure.

            // Arrange
            var expr = "=TimeOfDay()";

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
            Assert.AreEqual(DateTime.Now.Hour, result.Hour);
            Assert.AreEqual(DateTime.Now.Minute, result.Minute);
        }
    }
}
