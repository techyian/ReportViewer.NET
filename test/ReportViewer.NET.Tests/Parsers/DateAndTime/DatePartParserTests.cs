using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DatePartParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DatePartParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DatePart_Quarter_ShortString()
        {
            // Arrange
            var expr = "=DatePart(\"q\", \"January 15 2010\")";

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
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void DatePart_Month_ShortString()
        {
            // Arrange
            var expr = "=DatePart(\"m\", \"March 15 2010\")";

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
        public void DatePart_Week_ShortString_FirstDayOfWeek_FirstWeekOfYear_Int()
        {
            // Arrange

            // First week of year = FirstFullWeek
            // First day of week = Wednesday
            // For January 15 2010 this will result in a value of 2 returned from this expression.
            var expr = "=DatePart(\"ww\", \"January 15 2010\", 4, 3)";

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
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void DatePart_Week_ShortString_FirstDayOfWeek_FirstWeekOfYear_Enum()
        {
            // Arrange
            
            // First week of year = FirstFullWeek
            // First day of week = Wednesday
            // For January 15 2010 this will result in a value of 2 returned from this expression.
            var expr = "=DatePart(\"ww\", \"January 15 2010\", FirstDayOfWeek.Wednesday, FirstWeekOfYear.FirstFullWeek)";

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
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void DatePart_Month_ShortString_Nested_DateAdd()
        {
            // Arrange
            var expr = "=DatePart(\"m\", DateAdd(\"m\", 2, \"March 15 2010\"))";

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
        public void DatePart_Quarter_DateInterval()
        {
            // Arrange
            var expr = "=DatePart(DateInterval.Quarter, \"January 15 2010\")";

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
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void DatePart_Month_DateInterval()
        {
            // Arrange
            var expr = "=DatePart(DateInterval.Month, \"March 15 2010\")";

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
