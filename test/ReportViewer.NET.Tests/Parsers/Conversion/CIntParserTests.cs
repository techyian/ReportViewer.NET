using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.Conversion
{
    [TestClass]
    public class CIntParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public CIntParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void CInt_From_Double_Returns_Int()
        {
            // Arrange
            var expr = "=CInt(13.24)";

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
            Assert.AreEqual(13, result);            
        }

        [TestMethod]
        public void CInt_Addition_Returns_Int()
        {
            // Arrange
            var expr = "=CInt(13.24) + CInt(12)";

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
            Assert.AreEqual(25, result);
        }

        [TestMethod]
        public void CInt_Addition_With_Negative_CInt_Returns_Int()
        {
            // Arrange
            var expr = "=CInt(13.24) + CInt(-12)";

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
        public void CInt_Addition_With_Negative_RightSide_Returns_Int()
        {
            // Arrange
            var expr = "=CInt(13.24) + -12";

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
        public void CInt_Addition_With_Negative_LeftSide_Returns_Int()
        {
            // Arrange
            var expr = "=-12 + CInt(13.24)";

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
    }
}
