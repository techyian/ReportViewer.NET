using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Text
{
    [TestClass]
    public class InStrRevParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public InStrRevParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void InStrRev_Returns_Int_No_Start_Indx()
        {
            // InStr is not 0 based.

            // Arrange
            var expr = "=InStrRev(\"Brian\", \"ian\")";

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
        public void InStrRev_Returns_Int_With_Start_Indx_Text_Comparer()
        {
            // The start index is 0 based (VB method removes 1 from it)
            // Resulting index is not 0 based.

            // Arrange
            var expr = "=InStrRev(5, \"Brian Adams\", \"a\", 1)";

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
