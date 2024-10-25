using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System.Globalization;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DateStringParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DateStringParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DateString_Return_String()
        {          
            // Arrange
            var expr = "=FormatDateTime(DateString(), DateFormat.ShortDate)";

            // Act            
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            var now = DateTime.Now;
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
            Assert.AreEqual(now.ToShortDateString(), result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }

        [TestMethod]
        public void DateString_Return_String_Nested_DatePart()
        {
            // Arrange
            var now = DateTime.Now;
            var expr = "=MonthName(DatePart(\"m\", DateString()))";            
                                    
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
            Assert.AreEqual(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(now.Month), result);
        }
    }
}
