using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class WeekdayNameParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public WeekdayNameParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void WeekdayName_Returns_String()
        {
            // System defaults first day to Sunday, January 15 2010 is a Friday so we expect Friday to be returned.

            // Arrange
            var expr = "=WeekdayName(\"January 15, 2010 14:15:30\")";

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
            Assert.AreEqual("Friday", result);
        }

        [TestMethod]
        public void WeekdayName_Returns_String_First_Day_Tuesday()
        {
            // January 15 2010 is a Friday. Setting first day to Tuesday will make this return Sunday as there are 2 days between system default and Tuesday.

            // Arrange
            var expr = "=WeekdayName(\"January 15, 2010 14:15:30\", False, FirstDayOfWeek.Tuesday)";

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
            Assert.AreEqual("Sunday", result);
        }

        [TestMethod]
        public void WeekdayName_Returns_String_First_Day_Tuesday_Abbreviated()
        {
            // January 15 2010 is a Friday. Setting first day to Tuesday will make this return Sunday as there are 2 days between system default and Tuesday.

            // Arrange
            var expr = "=WeekdayName(\"January 15, 2010 14:15:30\", True, FirstDayOfWeek.Tuesday)";

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
            Assert.AreEqual("Sun", result);
        }
    }
}
