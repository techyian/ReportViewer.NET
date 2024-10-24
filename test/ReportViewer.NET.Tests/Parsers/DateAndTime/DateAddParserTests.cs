using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DateAddParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DateAddParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DateAdd_Day_ShortString()
        {
            // Arrange
            var expr = "=DateAdd(\"d\", 3, \"January 15 2010\")";
            var format = "yyyy-MM-dd";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                format
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("2010-01-18", result);
        }

        [TestMethod]
        public void DateAdd_Month_ShortString()
        {
            // Arrange
            var expr = "=DateAdd(\"m\", 3, \"January 15 2010\")";
            var format = "yyyy-MM-dd";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                format
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("2010-04-15", result);
        }

        [TestMethod]
        public void DateAdd_Day_ShortString_Nested()
        {
            // Arrange
            var expr = "=DateAdd(\"d\", 3, DateAdd(\"d\", 4, \"January 15 2010\"))";
            var format = "yyyy-MM-dd";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                format
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("2010-01-22", result);
        }

        [TestMethod]
        public void DateAdd_Day_DateInterval()
        {
            // Arrange
            var expr = "=DateAdd(DateInterval.Day, 3, \"January 15 2010\")";
            var format = "yyyy-MM-dd";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                format
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("2010-01-18", result);
        }

        [TestMethod]
        public void DateAdd_Month_DateInterval()
        {
            // Arrange
            var expr = "=DateAdd(DateInterval.Month, 3, \"January 15 2010\")";
            var format = "yyyy-MM-dd";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                format
            ).ExpressionAsString();

            // Assert
            Assert.AreEqual("2010-04-15", result);
        }
    }
}
