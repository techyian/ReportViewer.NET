using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Math
{
    [TestClass]
    public class RoundParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public RoundParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Round_With_String_Appended_Returns_String()
        {
            // Arrange
            var expr = "=\"Compliant: \" &amp; round(CDec(3)/(CDec(50))*100, System.MidpointRounding.AwayFromZero) &amp; \"%\"";

            var test = 3 / 50 * 100;

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                _report.DataSets[0].DataSetResults,
                null,
                1,
                _report.DataSets,
                _report.DataSets[0],
                null
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("Compliant: 6%", result);
        }
    }
}
