using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class WeekdayParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public WeekdayParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void Weekday_Returns_Integer()
        {
            // System defaults first day to Sunday, January 15 2010 is a Friday so this will be 6.

            // Arrange
            var expr = "=Weekday(\"January 15, 2010 14:15:30\")";

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
            Assert.AreEqual(6, result);
        }

        [TestMethod]
        public void Weekday_Returns_Integer_First_Day_Tuesday()
        {
            // January 15 2010 is a Friday. Setting first day to Tuesday will make this 4.

            // Arrange
            var expr = "=Weekday(\"January 15, 2010 14:15:30\", FirstDayOfWeek.Tuesday)";

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
            Assert.AreEqual(4, result);
        }
    }
}
