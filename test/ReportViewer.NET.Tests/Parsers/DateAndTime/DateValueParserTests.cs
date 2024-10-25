using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DateValueParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DateValueParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DateValue_Returns_Date_At_Midnight()
        {
            // Arrange
            var expr = "=DateValue(\"January 15, 2010 14:15:30\")";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsDateTime();

            // Assert
            Assert.AreEqual(new DateTime(2010, 1, 15, 0, 0, 0), result);
        }

        [TestMethod]
        public void DateValue_Returns_Date_At_Midnight_Nested_DateAdd()
        {
            // Arrange
            var expr = "=DateValue(DateAdd(\"d\", 3, \"January 15, 2010 14:15:30\"))";

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsDateTime();

            // Assert
            Assert.AreEqual(new DateTime(2010, 1, 18, 0, 0, 0), result);
        }

    }
}
