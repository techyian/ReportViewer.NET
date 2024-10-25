using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class MinuteParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public MinuteParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Minute_Returns_Integer()
        {
            // Arrange
            var expr = "=Minute(\"January 15, 2010 14:15:30\")";

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
            Assert.AreEqual(15, result);
        }

        [TestMethod]
        public void Minute_Returns_Integer_Nested_DateAdd()
        {
            // Arrange
            var expr = "=Minute(DateAdd(\"n\", 3, \"January 15, 2010 14:15:30\"))";

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
            Assert.AreEqual(18, result);
        }
    }
}
