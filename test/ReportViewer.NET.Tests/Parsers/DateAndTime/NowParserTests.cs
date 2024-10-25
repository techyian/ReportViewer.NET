using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class NowParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public NowParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Now_Returns_DateTime()
        {
            // Limiting to hour and minute to prevent test failure.

            // Arrange
            var expr = "=Now()";

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
