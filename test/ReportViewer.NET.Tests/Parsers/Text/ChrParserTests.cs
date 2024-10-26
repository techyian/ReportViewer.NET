using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Text
{
    [TestClass]
    public class ChrParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public ChrParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Chr_Returns_Char()
        {
            // Arrange
            var expr = "=Chr(66)";

            // Act
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
            Assert.AreEqual("B", result);
        }

        [TestMethod]
        public void Chr_Returns_Char_Nested()
        {
            // Arrange
            var expr = "=Chr(Asc(MonthName(1)))";

            // Act
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
            Assert.AreEqual("J", result);
        }
    }
}
