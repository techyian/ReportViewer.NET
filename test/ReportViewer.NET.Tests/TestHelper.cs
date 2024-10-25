using ReportViewer.NET.DataObjects;
using System.Reflection;

namespace ReportViewer.NET.Tests
{
    public class TestHelper
    {
        public static ReportRDL PrimeReport()
        {
            var reportHandler = new ReportHandler();
            var rdlXml = GetRdl("Format text.rdl");
            reportHandler.RegisterDataSource("TextDataSource", "");
            reportHandler.RegisterRdlFromString("Format text", rdlXml);
            return reportHandler.LoadReport("Format text", new ReportParameters());                        
        }

        private static string GetRdl(string filename)
        {
            var assembly = Assembly.Load("ReportViewer.NET.Tests");
            var resourceName = GetEmbeddedResourceNames().Where(c => c.Contains(filename)).First();

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                var result = reader.ReadToEnd();

                if (string.IsNullOrEmpty(result))
                {
                    throw new NullReferenceException("RDL file was empty.");
                }

                return result;
            }
        }

        private static string[] GetEmbeddedResourceNames()
        {
            var assembly = Assembly.Load("ReportViewer.NET.Tests");
            return assembly.GetManifestResourceNames();
        }
    }
}
