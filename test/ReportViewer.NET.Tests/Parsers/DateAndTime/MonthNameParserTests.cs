using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class MonthNameParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public MonthNameParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void MonthName_Returns_String()
        {
            // Arrange
            var expr = "=MonthName(1)";

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
            Assert.AreEqual("January", result);
        }

        [TestMethod]
        public void MonthName_Returns_String_Abbreviate()
        {
            // Arrange
            var expr = "=MonthName(1, True)";

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
            Assert.AreEqual("Jan", result);
        }

        [TestMethod]
        public void MonthName_Returns_String_Explicit_Non_Abbreviate()
        {
            // Arrange
            var expr = "=MonthName(1, False)";

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
            Assert.AreEqual("January", result);
        }

        [TestMethod]
        public void MonthName_Returns_String_Nested_Month_And_DateAdd()
        {
            // Arrange
            var expr = "=MonthName(Month(DateAdd(\"m\", 3, \"January 15, 2010 14:15:30\")))";

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
            Assert.AreEqual("April", result);
        }
    }
}
