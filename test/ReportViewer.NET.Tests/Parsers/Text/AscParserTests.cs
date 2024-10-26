using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Text
{
    [TestClass]
    public class AscParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public AscParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Asc_Returns_Integer()
        {
            // Arrange
            var expr = "=Asc(\"Bob\")";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsInt();

            // Assert
            Assert.AreEqual(66, result);
        }

        [TestMethod]
        public void Asc_Returns_Integer_Nested()
        {
            // Arrange
            var expr = "=Asc(MonthName(1))";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsInt();

            // Assert
            Assert.AreEqual(74, result);
        }

    }
}
