using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;
using System.Globalization;


namespace ReportViewer.NET.Tests.Parsers.DateAndTime
{
    [TestClass]
    public class FormatDateTimeParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public FormatDateTimeParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void FormatDateTime_Return_ShortDate_String()
        {
            // Arrange
            var expr = "=FormatDateTime(\"January 15, 2010\", DateFormat.ShortDate)";

            // Act            
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
            Assert.AreEqual("15/01/2010", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }

        [TestMethod]
        public void FormatDateTime_Return_LongDate_String()
        {
            // Arrange
            var expr = "=FormatDateTime(\"January 15, 2010\", DateFormat.LongDate)";

            // Act            
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
            Assert.AreEqual("15 January 2010", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }

        [TestMethod]
        public void FormatDateTime_Return_ShortDate_String_Nested_DateAdd()
        {
            // Arrange
            var expr = "=FormatDateTime(DateAdd(\"d\", 3, \"January 15, 2010\"), DateFormat.ShortDate)";

            // Act            
            var cultureInfo = new CultureInfo("en-GB");
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

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
            Assert.AreEqual("18/01/2010", result);

            CultureInfo.DefaultThreadCurrentCulture = originalCulture;
            CultureInfo.DefaultThreadCurrentUICulture = originalCulture;
        }
    }
}
