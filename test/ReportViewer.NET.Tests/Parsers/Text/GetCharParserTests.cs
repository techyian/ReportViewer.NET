using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Text
{
    [TestClass]
    public class GetCharParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public GetCharParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void GetChar_Returns_String()
        {
            // The Index parameter of GetChar is not 0 based.

            // Arrange
            var expr = "=GetChar(\"Brian\", 2)";

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
            Assert.AreEqual("r", result);
        }
    }
}
