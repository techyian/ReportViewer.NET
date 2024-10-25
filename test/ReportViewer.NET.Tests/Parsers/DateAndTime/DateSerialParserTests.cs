using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System.Globalization;

namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class DateSerialParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public DateSerialParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void DateSerial_Returns_DateTime()
        {
            // Arrange
            var expr = "=DateSerial(DatePart(\"yyyy\",\"January 15, 2010 14:30:15\")-10, DatePart(\"m\",\"January 15, 2010 14:30:15\")+3,DatePart(\"d\",\"January 15, 2010 14:30:15\")-1)";
            
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
            Assert.AreEqual(new DateTime(2000, 4, 14, 0, 0, 0), result);
        }

        [TestMethod]
        public void DateSerial_Returns_String()
        {
            // Arrange
            var expr = "=\"DateSerial parsed as \" & FormatDateTime(DateSerial(DatePart(\"yyyy\",\"January 15, 2010 14:30:15\")-10, DatePart(\"m\",\"January 15, 2010 14:30:15\")+3,DatePart(\"d\",\"January 15, 2010 14:30:15\")-1), DateFormat.ShortDate)";
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
            Assert.AreEqual("DateSerial parsed as 14/04/2000", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }
    }
}
