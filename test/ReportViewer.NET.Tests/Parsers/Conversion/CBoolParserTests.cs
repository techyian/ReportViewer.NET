using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Conversion
{
    [TestClass]
    public class CBoolParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public CBoolParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void CBool_From_Char_Comparison_Returns_Boolean()
        {
            // Arrange
            var expr = "=CBool(GetChar(\"Brian\", 1) = \'B\')";

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
            Assert.AreEqual(true, (bool)result);
        }

        [TestMethod]
        public void CBool_Nested_IIf_Returns_Boolean()
        {
            // Arrange
            var expr = "=CBool(IIf(Hour(Now()) >= 12 And Hour(Now()) < 18,\"Afternoon\", IIf(Hour(Now()) > 5 And Hour(Now()) < 12, \"Morning\", \"Evening\")) = \"Afternoon\")";

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
            var now = DateTime.Now;

            if (now.Hour >= 12 && now.Hour < 18)
            {
                Assert.AreEqual(true, (bool)result);
            }
            else if (now.Hour > 5 && now.Hour < 12)
            {
                Assert.AreEqual(false, (bool)result);
            }
            else
            {
                Assert.AreEqual(false, (bool)result);
            }
        }
    }
}
