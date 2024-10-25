using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class TimerParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public TimerParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Timer_Returns_Double()
        {
            // Arrange
            var expr = "=Timer()";
            var secondsSinceMidnight = (DateTime.Now - DateTime.Now.Date).TotalSeconds;

            // Act
            var result = _expressionParser.ParseReportExpressionString(
                expr,
                null,
                null,
                1,
                _report.DataSets,
                null,
                null
            ).ExpressionAsDouble();

            // Assert
            // Allow slight margin.
            Assert.IsTrue(result > secondsSinceMidnight && result < (secondsSinceMidnight + 2));            
        }
    }
}
