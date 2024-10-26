using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Text
{
    [TestClass]
    public class ChrWParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public ChrWParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void ChrW_Returns_Char()
        {
            // Arrange
            var expr = "=ChrW(217)";

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
            Assert.AreEqual("\u00D9", result);
        }

        [TestMethod]
        public void ChrW_Returns_Char_Extended_Unicode()
        {
            // Arrange
            var expr = "=ChrW(260)";

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
            Assert.AreEqual("\u0104", result);
        }

        [TestMethod]
        public void Chr_Returns_Char_Nested()
        {
            // Arrange
            var expr = "=ChrW(AscW(\"\u00D9\"))";

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
            Assert.AreEqual("\u00D9", result);
        }
    }
}
