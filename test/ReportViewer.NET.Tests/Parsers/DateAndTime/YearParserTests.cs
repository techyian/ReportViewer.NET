using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class YearParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public YearParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Year_Returns_Integer()
        {
            // Arrange
            var expr = "=Year(\"January 15, 2010 14:15:30\")";

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
            Assert.AreEqual(2010, result);
        }

        [TestMethod]
        public void Day_Returns_Integer_Nested_DateAdd()
        {
            // Arrange
            var expr = "=Year(DateAdd(\"yyyy\", 3, \"January 15, 2010 14:15:30\"))";

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
            Assert.AreEqual(2013, result);
        }
    }
}
