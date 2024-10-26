using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET.Tests.Parsers.ProgramFlow
{
    [TestClass]
    public class IfParserTests
    {
        private ExpressionParser _expressionParser;
        private ReportRDL _report;

        public IfParserTests()
        {
            var report = TestHelper.PrimeReport();

            _report = report;
            _expressionParser = new ExpressionParser(report);
        }

        [TestMethod]
        public void If_Value_Time_Of_Day_Nested()
        {
            // Arrange
            var expr = "=IIf(Hour(Now()) >= 12 And Hour(Now()) < 18,\"Afternoon\", IIf(Hour(Now()) > 5 And Hour(Now()) < 12, \"Morning\", \"Evening\"))";

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
            var now = DateTime.Now;

            if (now.Hour >= 12 && now.Hour < 18)
            {
                Assert.AreEqual("Afternoon", result);
            }
            else if (now.Hour > 5 && now.Hour < 12)
            {
                Assert.AreEqual("Morning", result);
            }
            else 
            {
                Assert.AreEqual("Evening", result);
            }            
        }
    }
}
