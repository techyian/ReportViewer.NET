using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class CDateParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public CDateParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void CDate_From_Date_Returns_Date()
        {
            // Arrange
            var expr = "=CDate(Now())";

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
            Assert.AreEqual(DateTime.Now.Hour, result.Hour);
            Assert.AreEqual(DateTime.Now.Minute, result.Minute);
        }

        [TestMethod]
        public void CDate_From_String_Returns_Date()
        {
            // Arrange
            var expr = "=CDate(\"January 15, 2010\")";

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
            Assert.AreEqual(new DateTime(2010, 1, 15), result);            
        }
    }
}
