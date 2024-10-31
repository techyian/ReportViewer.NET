using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Conversion
{
    [TestClass]
    public class CCharParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public CCharParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void CChar_From_Int_Returns_Char()
        {
            // Arrange
            var expr = "=CChar(1)";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            );

            // Assert
            Assert.AreEqual('\u0001', result);
        }

        [TestMethod]
        public void CChar_From_Char_Returns_Char()
        {
            // Arrange
            var expr = "=CChar('a')";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            );

            // Assert
            Assert.AreEqual('a', result);
        }
    }
}
