using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class TimeSerialParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public TimeSerialParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void TimeSerial_Returns_DateTime()
        {
            // Arrange
            var expr = "=TimeSerial(DatePart(\"h\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday),DatePart(\"n\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday),DatePart(\"s\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday))";
            
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
            // Allow slight margin.
            Assert.AreEqual(new DateTime(1, 1, 1, 10, 30, 13), result);
        }

        [TestMethod]
        public void TimeSerial_Returns_String()
        {
            // Arrange
            var expr = "=\"TimeSerial parsed as \" & FormatDateTime(TimeSerial(DatePart(\"h\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday),DatePart(\"n\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday),DatePart(\"s\",\"January 15 2010 10:30:13\", FirstDayOfWeek.Monday)), DateFormat.ShortDate)";

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
            // Allow slight margin.
            Assert.AreEqual("TimeSerial parsed as 01/01/0001", result);
        }
    }
}
