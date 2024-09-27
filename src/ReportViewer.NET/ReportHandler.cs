using Microsoft.AspNetCore.Html;
using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ReportViewer.NET
{
    public class ReportHandler : IReportHandler
    {        
        public static XNamespace ReportDesigner = "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";

        private List<ReportRDL> _reportRdls;
        private List<DataSource> _dataSources;
        private List<DataSet> _sharedDataSets;
        
        public ReportHandler()
        {
            _reportRdls = new List<ReportRDL>();
            _dataSources = new List<DataSource>();
            _sharedDataSets = new List<DataSet>();
        }

        public void RegisterRdlFromFile(string rdlName, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Requested file path does not exist.");
            }

            if (_dataSources == null)
            {
                throw new InvalidDataException("Data sources have not been initialised.");
            }

            using (var sr = new StreamReader(filePath))
            {
                var xml = XDocument.Load(sr);
                var rdl = new ReportRDL()
                {
                    Name = rdlName,
                    Xml = xml
                };

                _reportRdls.Add(rdl);
            }
        }

        public void RegisterRdlFromString(string rdlName, string rdlXml)
        {
            using (var sr = new StringReader(rdlXml))
            {
                var xml = XDocument.Load(sr);
                var rdl = new ReportRDL()
                {
                    Name = rdlName,
                    Xml = xml
                };

                _reportRdls.Add(rdl);
            }
        }

        public void LoadReport(string rdlName, ReportParameters userProvidedParameters)
        {
            var idx = _reportRdls.FindIndex(r => r.Name == rdlName);
            var rdl = _reportRdls.First(r => r.Name == rdlName);
            
            _reportRdls[idx] = this.ParseXml(rdl.Xml, rdlName, userProvidedParameters);
        }

        public void RegisterDataSource(string name, string connectionString, string datasourceReference = null)
        {            
            _dataSources.Add(new DataSource(name, connectionString, datasourceReference));
        }

        public void RegisterSharedDataSet(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Requested file path does not exist.");
            }

            using (var sr = new StreamReader(filePath))
            {
                var xml = XDocument.Load(sr);

                XPathNavigator xpn = xml.CreateNavigator();
                xpn.MoveToFollowing(XPathNodeType.Element);
                IDictionary<string, string> namespaces = xpn.GetNamespacesInScope(XmlNamespaceScope.All);

                XNamespace ns = namespaces[""];
                var datasetElements = xml.Root.Elements(ns + "DataSet");
                var dataSets = this.ProcessDataSets(xml.Root, datasetElements, ns);

                _sharedDataSets.AddRange(dataSets);
            }
        }

        public Task<HtmlString> PublishReportParameters(string report, IEnumerable<ReportParameter> userProvidedParameters)
        {
            var rdl = _reportRdls.FirstOrDefault(r => r.Name == report);

            if (rdl == null)
            {
                throw new NullReferenceException("Requested RDL not registered.");
            }

            var layoutProvider = new LayoutProvider();

            return layoutProvider.PublishReportParameters(rdl, userProvidedParameters);
        }

        public Task<HtmlString> PublishReportOutput(string report, IEnumerable<ReportParameter> userProvidedParameters, IEnumerable<string> requestedVisible)
        {
            var rdl = _reportRdls.FirstOrDefault(r => r.Name == report);

            if (rdl == null)
            {
                throw new NullReferenceException("Requested RDL not registered.");
            }

            var layoutProvider = new LayoutProvider();

            return layoutProvider.PublishReportOutput(rdl, userProvidedParameters, requestedVisible ?? Enumerable.Empty<string>());
        }

        private ReportRDL ParseXml(XDocument xml, string name, ReportParameters userProvidedParameters)
        {            
            var reportRdl = new ReportRDL();
                        
            XPathNavigator xpn = xml.CreateNavigator();
            xpn.MoveToFollowing(XPathNodeType.Element);
            IDictionary<string, string> namespaces = xpn.GetNamespacesInScope(XmlNamespaceScope.All);

            reportRdl.Namespace = namespaces[""];
            reportRdl.CurrentRegisteredReports = _reportRdls;
            reportRdl.Name = name;
            reportRdl.UserProvidedParameters = userProvidedParameters.Parameters;

            // Validate data sources.
            var dataSourceElements = xml.Root.Descendants(reportRdl.Namespace + "DataSources").Elements(reportRdl.Namespace + "DataSource");

            if (!this.ValidateDataSource(dataSourceElements))
            {
                throw new InvalidOperationException("Found data source which has not been configured.");                
            }

            // Fetch data sets.
            var datasetElements = xml.Root.Descendants(reportRdl.Namespace + "DataSets").SelectMany(e => e.Elements(reportRdl.Namespace + "DataSet"));
            var dataSets = this.ProcessDataSets(xml.Root, datasetElements, reportRdl.Namespace);
            var reportParameters = this.ProcessReportParameters(xml.Root, dataSets, reportRdl.Namespace);
            var embeddedImages = this.ProcessEmbeddedImages(xml.Root, reportRdl.Namespace);

            reportRdl.DataSources = _dataSources;
            reportRdl.ReportParameters = reportParameters;
            reportRdl.EmbeddedImages = embeddedImages;
            reportRdl.DataSets = dataSets;

            reportRdl.DataSets.AddRange(_sharedDataSets);

            var reportBodyItems = this.ProcessReportItems(xml.Root.Descendants(reportRdl.Namespace + "Body").Elements(reportRdl.Namespace + "ReportItems"), dataSets, reportRdl);
            var reportFooterItems = this.ProcessReportItems(xml.Root.Descendants(reportRdl.Namespace + "PageFooter").Elements(reportRdl.Namespace + "ReportItems"), dataSets, reportRdl);

            reportRdl.ReportBodyItems = reportBodyItems;
            reportRdl.ReportFooterItems = reportFooterItems;            
            reportRdl.ReportWidth = xml.Root.Descendants(reportRdl.Namespace + "ReportSections").Elements(reportRdl.Namespace + "ReportSection").Elements(reportRdl.Namespace + "Width").FirstOrDefault()?.Value;
            
            return reportRdl;
        }

        private bool ValidateDataSource(IEnumerable<XElement> dataSources)
        {
            foreach (var datasource in dataSources)
            {
                if (!_dataSources.Any(ds => ds.Name == datasource.Attribute("Name").Value))
                {                    
                    return false;
                }
            }

            return true;
        }

        private List<DataSet> ProcessDataSets(XElement root, IEnumerable<XElement> datasetElements, XNamespace ns)
        {
            var datasets = new List<DataSet>();
            var reportParameterElements = root.Descendants(ns + "ReportParameters").Elements(ns + "ReportParameter");

            foreach (var ds in datasetElements)
            {
                var datasetObj = new DataSet();

                var sds = ds.Element(ns + "SharedDataSet")?.Element(ns + "SharedDataSetReference")?.Value;

                // Do we reference a shared dataset?
                if (!string.IsNullOrEmpty(sds))
                {
                    if (!_sharedDataSets.Any(ds => ds.Name == sds))
                    {
                        throw new NullReferenceException("Required shared dataset not loaded.");
                    }

                    datasets.Add(_sharedDataSets.First(sds => sds.Name == sds.Name));
                }

                datasetObj.Name = ds.Attribute("Name").Value;

                // Process Query
                var queryElement = ds.Element(ns + "Query");

                if (queryElement != null)
                {                    
                    datasetObj.Query = new DataSetQuery();

                    var queryParams = queryElement.Element(ns + "QueryParameters")?.Elements(ns + "QueryParameter");

                    if (queryParams != null)
                    {
                        datasetObj.Query.QueryParameters = new List<DataSetQueryParameter>();

                        foreach (var qp in queryParams)
                        {
                            datasetObj.Query.QueryParameters.Add(new DataSetQueryParameter
                            {
                                Name = qp.Attribute("Name")?.Value                                
                                // TODO: Add default value.
                            });
                        }
                    }

                    datasetObj.Query.DataSourceName = queryElement.Element(ns + "DataSourceName")?.Value;
                    datasetObj.Query.DataSourceReference = queryElement.Element(ns + "DataSourceReference")?.Value;
                    datasetObj.Query.CommandText = queryElement.Element(ns + "CommandText")?.Value;
                    datasetObj.Query.CommandType = queryElement.Element(ns + "CommandType")?.Value;
                }

                // Process Fields
                var fieldElements = ds.Element(ns + "Fields")?.Elements(ns + "Field");

                if (fieldElements != null)
                {
                    datasetObj.Fields = new List<DataSetField>();

                    foreach (var field in fieldElements)
                    {                        
                        var typeName = field.Element(ReportDesigner + "TypeName")?.Value;
                        
                        var fieldObj = new DataSetField()
                        {
                            Name = field.Attribute("Name")?.Value,
                            TypeName = !string.IsNullOrEmpty(typeName) ? Type.GetType(typeName) : default,
                            DataField = field.Element(ns + "DataField")?.Value,
                            Value = field.Element(ns + "Value")?.Value
                        };
                                                                        
                        if (!string.IsNullOrEmpty(fieldObj.Name))
                        {
                            var reportParameterElement = reportParameterElements.FirstOrDefault(e => e.Attribute("Name")?.Value == fieldObj.Name);

                            if (reportParameterElement != null)
                            {
                                fieldObj.Label = reportParameterElement.Element(ns + "Prompt")?.Value;
                            }
                        }

                        datasetObj.Fields.Add(fieldObj);
                    }
                }

                datasets.Add(datasetObj);
            }

            return datasets;
        }

        private List<ReportParameter> ProcessReportParameters(XElement root, IEnumerable<DataSet> datasets, XNamespace ns)
        {
            var reportParamList = new List<ReportParameter>();
            var reportParameterElements = root.Descendants(ns + "ReportParameters").Elements(ns + "ReportParameter");

            foreach (var rp in reportParameterElements)
            {
                var reportParamObj = new ReportParameter()
                {
                    Name = rp.Attribute("Name")?.Value,
                    DataType = rp.Element(ns + "DataType")?.Value,
                    Nullable = rp.Element(ns + "Nullable")?.Value == "true",
                    Prompt = rp.Element(ns + "Prompt")?.Value,
                    MultiValue = rp.Element(ns + "MultiValue")?.Value == "true",
                    DefaultValue = rp.Element(ns + "DefaultValue")?.Element(ns + "Values")?.Element(ns + "Value")?.Value
                };

                if (!string.IsNullOrEmpty(reportParamObj.DataType))
                {
                    switch (reportParamObj.DataType)
                    {
                        case "String":                            
                            reportParamObj.TypeName = typeof(string);
                            break;
                        case "DateTime":
                            reportParamObj.TypeName = reportParamObj.Nullable ? typeof(DateTime?) : typeof(DateTime);
                            break;
                        case "Boolean":
                            reportParamObj.TypeName = reportParamObj.Nullable ? typeof(bool?) : typeof(bool);
                            break;
                    }
                }

                var dsRef = rp.Element(ns + "ValidValues")?.Element(ns + "DataSetReference");

                if (dsRef != null)
                {
                    reportParamObj.DataSetReference = new DataSetReference()
                    {
                        DataSetName = dsRef.Element(ns + "DataSetName")?.Value,
                        ValueField = dsRef.Element(ns + "ValueField")?.Value,
                        LabelField = dsRef.Element(ns + "LabelField")?.Value                        
                    };

                    reportParamObj.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == reportParamObj.DataSetReference.DataSetName);
                }

                reportParamList.Add(reportParamObj);
            }

            return reportParamList;
        }

        private List<ReportItem> ProcessReportItems(IEnumerable<XElement> reportItemElements, IEnumerable<DataSet> datasets, ReportRDL rdl)
        {
            return ReportItem.ParseElements(reportItemElements, rdl, datasets, null, null).ToList();
        }

        private List<EmbeddedImage> ProcessEmbeddedImages(XElement root, XNamespace ns)
        {
            var embeddedImages = new List<EmbeddedImage>();
            var reportEmbeddedImages = root.Descendants(ns + "EmbeddedImages").Elements(ns + "EmbeddedImage");

            if (reportEmbeddedImages != null)
            {
                foreach (var ei in reportEmbeddedImages)
                {
                    embeddedImages.Add(new EmbeddedImage
                    {
                        Name = ei.Attribute("Name")?.Value,
                        MimeType = ei.Element(ns + "MIMEType")?.Value,
                        ImageData = ei.Element(ns + "ImageData")?.Value
                    });                    
                }
            }

            return embeddedImages;
        }
    }
}
