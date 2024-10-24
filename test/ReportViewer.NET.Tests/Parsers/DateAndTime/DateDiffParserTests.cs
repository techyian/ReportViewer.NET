using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DateDiffParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DateDiffParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DateDiff_Month_ShortString()
        {
            // Arrange
            var expr = "=DateDiff(\"m\", \"January 15 2010\", \"July 5 2010\")";

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
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void DateDiff_Year_ShortString()
        {
            // Arrange
            var expr = "=DateDiff(\"yyyy\", \"January 15 2010\", \"July 5 2013\")";

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
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void DateDiff_Month_ShortString_Nested_DateAdd()
        {
            // Arrange
            var expr = "=DateDiff(\"m\", DateAdd(\"m\", 2, \"January 15 2010\"), \"July 5 2010\")";

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
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void DateDiff_Month_DateInterval()
        {
            // Arrange
            var expr = "=DateDiff(DateInterval.Month, \"January 15 2010\", \"July 5 2010\")";

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
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void DateDiff_Year_DateInterval()
        {
            // Arrange
            var expr = "=DateDiff(DateInterval.Year, \"January 15 2010\", \"July 5 2013\")";

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
            Assert.AreEqual(3, result);
        }
    }
}
