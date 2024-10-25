using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class SecondParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public SecondParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Second_Returns_Integer()
        {
            // Arrange
            var expr = "=Second(\"January 15, 2010 14:15:30\")";

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
            Assert.AreEqual(30, result);
        }

        [TestMethod]
        public void Second_Returns_Integer_Nested_DateAdd()
        {
            // Arrange
            var expr = "=Second(DateAdd(\"s\", 3, \"January 15, 2010 14:15:30\"))";

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
            Assert.AreEqual(33, result);
        }
    }
}
