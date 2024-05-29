using Microsoft.AspNetCore.Html;
using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReportViewer.NET
{
    public class ReportViewer : IReportViewer
    {        
        private readonly XNamespace _ns1 = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";
        private readonly XNamespace _ns2 = "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner";

        private List<ReportRDL> _reportRdls;
        private List<DataSource> _dataSources;
        
        public ReportViewer()
        {
            _reportRdls = new List<ReportRDL>();
            _dataSources = new List<DataSource>();
        }

        public ReportRDL RegisterRdl(string filepath)
        {            
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException("Requested file path does not exist.");                
            }

            if (_dataSources == null)
            {
                throw new InvalidDataException("Data sources have not been initialised.");
            }

            using (var sr = new StreamReader(filepath))
            {
                var xml = XDocument.Load(sr);
                var rdl = this.ParseXml(xml);

                rdl.Name = Path.GetFileName(filepath);

                _reportRdls.Add(rdl);
            }

            return null;
        }

        public void RegisterDataSource(string name, string connectionString)
        {            
            _dataSources.Add(new DataSource(name, connectionString));
        }

        public Task<HtmlString> PublishReportParameters(string report, IEnumerable<ReportParameter> userProvidedParameters)
        {
            var rdl = _reportRdls.FirstOrDefault(r => r.Name == report);

            if (rdl == null)
            {
                throw new NullReferenceException("Requested RDL not registered.");
            }

            var layoutProvider = new LayoutProvider(rdl);

            return layoutProvider.PublishReportParameters(userProvidedParameters);
        }

        public Task<HtmlString> PublishReportOutput(string report, IEnumerable<ReportParameter> userProvidedParameters)
        {
            var rdl = _reportRdls.FirstOrDefault(r => r.Name == report);

            if (rdl == null)
            {
                throw new NullReferenceException("Requested RDL not registered.");
            }

            var layoutProvider = new LayoutProvider(rdl);

            return layoutProvider.PublishReportOutput(userProvidedParameters);
        }

        private ReportRDL ParseXml(XDocument xml)
        {            
            var reportRdl = new ReportRDL();
                        
            // Validate xml.
            if (!this.ValidateRdlNamespace(xml.Root.Attributes()))
            {
                throw new InvalidDataException("Root namespace is invalid");                
            }

            // Validate data sources.
            var dataSourceElements = xml.Root.Descendants(_ns1 + "DataSources").Elements(_ns1 + "DataSource");

            if (!this.ValidateDataSource(dataSourceElements))
            {
                throw new InvalidOperationException("Found data source which has not been configured.");                
            }

            // Fetch data sets.
            var datasetElements = xml.Root.Descendants(_ns1 + "DataSets").SelectMany(e => e.Elements(_ns1 + "DataSet"));
            var dataSets = this.ProcessDataSets(xml.Root, datasetElements);
            var reportParameters = this.ProcessReportParameters(xml.Root, dataSets);
            var reportItems = this.ProcessReportItems(xml.Root, dataSets);

            reportRdl.DataSources = _dataSources;
            reportRdl.DataSets = dataSets;
            reportRdl.ReportParameters = reportParameters;
            reportRdl.ReportItems = reportItems;

            return reportRdl;
        }

        private bool ValidateRdlNamespace(IEnumerable<XAttribute> attributes)
        {
            var ns1 = ("xmlns", "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition");
            var ns2 = ("xmlns:rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner");

            if (!attributes.Any(a => a.Value == ns1.Item2) ||
                !attributes.Any(a => a.Value == ns2.Item2))
            {
                return false;
            }

            return true;
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

        private List<DataSet> ProcessDataSets(XElement root, IEnumerable<XElement> datasetElements)
        {
            var datasets = new List<DataSet>();
            var reportParameterElements = root.Descendants(_ns1 + "ReportParameters").Elements(_ns1 + "ReportParameter");

            foreach (var ds in datasetElements)
            {
                var datasetObj = new DataSet();

                datasetObj.Name = ds.Attribute("Name").Value;

                // Process Query
                var queryElement = ds.Element(_ns1 + "Query");

                if (queryElement != null)
                {                    
                    datasetObj.Query = new DataSetQuery();

                    var queryParams = queryElement.Element(_ns1 + "QueryParameters")?.Elements(_ns1 + "QueryParameter");

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

                    datasetObj.Query.DataSourceName = queryElement.Element(_ns1 + "DataSourceName")?.Value;
                    datasetObj.Query.CommandText = queryElement.Element(_ns1 + "CommandText")?.Value;
                }

                // Process Fields
                var fieldElements = ds.Element(_ns1 + "Fields")?.Elements(_ns1 + "Field");

                if (fieldElements != null)
                {
                    datasetObj.Fields = new List<DataSetField>();

                    foreach (var field in fieldElements)
                    {
                        var fieldName = field.Attribute("Name")?.Value;
                        var typeName = field.Element(_ns2 + "TypeName")?.Value;
                        var fieldObj = new DataSetField()
                        {
                            Name = fieldName,
                            TypeName = !string.IsNullOrEmpty(typeName) ? Type.GetType(typeName) : default
                        };
                                                                        
                        if (!string.IsNullOrEmpty(fieldName))
                        {
                            var reportParameterElement = reportParameterElements.FirstOrDefault(e => e.Attribute("Name")?.Value == fieldName);

                            if (reportParameterElement != null)
                            {
                                fieldObj.Label = reportParameterElement.Element(_ns1 + "Prompt")?.Value;
                            }
                        }

                        datasetObj.Fields.Add(fieldObj);
                    }
                }

                datasets.Add(datasetObj);
            }

            return datasets;
        }

        private List<ReportParameter> ProcessReportParameters(XElement root, IEnumerable<DataSet> datasets)
        {
            var reportParamList = new List<ReportParameter>();
            var reportParameterElements = root.Descendants(_ns1 + "ReportParameters").Elements(_ns1 + "ReportParameter");

            foreach (var rp in reportParameterElements)
            {
                var reportParamObj = new ReportParameter()
                {
                    Name = rp.Attribute("Name")?.Value,
                    DataType = rp.Element(_ns1 + "DataType")?.Value,
                    Nullable = rp.Element(_ns1 + "Nullable")?.Value == "true",
                    Prompt = rp.Element(_ns1 + "Prompt")?.Value,
                    MultiValue = rp.Element(_ns1 + "MultiValue")?.Value == "true",
                    DefaultValue = rp.Element(_ns1 + "DefaultValue")?.Element(_ns1 + "Values")?.Element(_ns1 + "Value")?.Value
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

                var dsRef = rp.Element(_ns1 + "ValidValues")?.Element(_ns1 + "DataSetReference");

                if (dsRef != null)
                {
                    reportParamObj.DataSetReference = new DataSetReference()
                    {
                        DataSetName = dsRef.Element(_ns1 + "DataSetName")?.Value,
                        ValueField = dsRef.Element(_ns1 + "ValueField")?.Value,
                        LabelField = dsRef.Element(_ns1 + "LabelField")?.Value                        
                    };

                    reportParamObj.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == reportParamObj.DataSetReference.DataSetName);
                }

                reportParamList.Add(reportParamObj);
            }

            return reportParamList;
        }

        private List<ReportItem> ProcessReportItems(XElement root, IEnumerable<DataSet> datasets)
        {
            var reportItemList = new List<ReportItem>();
            var reportItemElements = root.Descendants(_ns1 + "ReportItems");

            if (reportItemElements != null)
            {
                foreach (var ri in reportItemElements)
                {
                    var tablixElements = ri.Elements(_ns1 + "Tablix");

                    if (tablixElements != null)
                    {
                        foreach (var te in tablixElements)
                        {
                            reportItemList.Add(this.ProcessTablixElement(te, datasets));
                        }
                    }

                    var textboxElements = ri.Elements(_ns1 + "Textbox");

                    if (textboxElements != null)
                    {
                        foreach (var tb in textboxElements)
                        {
                            reportItemList.Add(this.ProcessTextbox(tb));
                        }
                    }
                }
            }

            return reportItemList;
        }

        private Tablix ProcessTablixElement(XElement tablix, IEnumerable<DataSet> datasets)
        {
            var tablixObj = new Tablix();
            var tablixBody = new TablixBody();

            var columns = tablix.Element(_ns1 + "TablixBody").Elements(_ns1 + "TablixColumns").Elements(_ns1 + "TablixColumn");
            var rows = tablix.Element(_ns1 + "TablixBody").Elements(_ns1 + "TablixRows").Elements(_ns1 + "TablixRow");

            tablixObj.DataSetName = tablix.Element(_ns1 + "DataSetName")?.Value;            
            tablixObj.Hidden = tablix.Element(_ns1 + "Visibility")?.Element(_ns1 + "Hidden")?.Value == "true";
            tablixObj.ToggleItem = tablix.Element(_ns1 + "Visibility")?.Element(_ns1 + "ToggleItem")?.Value;

            tablixObj.Style = this.ProcessStyle(tablix.Element(_ns1 + "Style"));
            tablixObj.Style.Top = tablix.Element(_ns1 + "Top")?.Value;
            tablixObj.Style.Left = tablix.Element(_ns1 + "Left")?.Value;
            tablixObj.Style.Height = tablix.Element(_ns1 + "Height")?.Value;
            tablixObj.Style.Width = tablix.Element(_ns1 + "Width")?.Value;

            if (!string.IsNullOrEmpty(tablixObj.DataSetName))
            {
                tablixObj.DataSetReference = new DataSetReference()
                {
                    DataSetName = tablixObj.DataSetName
                };

                tablixObj.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == tablixObj.DataSetReference.DataSetName);
            }

            if (columns != null)
            {
                tablixBody.TablixColumns = new List<TablixColumn>();

                foreach (var c in columns)
                {
                    tablixBody.TablixColumns.Add(new TablixColumn
                    {
                        Width = c.Element(_ns1 + "Width")?.Value
                    });
                }
            }

            if (rows != null)
            {
                tablixBody.TablixRows = new List<TablixRow>();

                foreach (var r in rows)
                {
                    var tablixRow = new TablixRow
                    {
                        Height = r.Element(_ns1 + "Height")?.Value
                    };
                    tablixRow.TablixCells = new List<TablixCell>();

                    var cells = r.Elements(_ns1 + "TablixCells").Elements(_ns1 + "TablixCell");

                    if (cells != null)
                    {
                        foreach (var c in cells)
                        {
                            tablixRow.TablixCells.Add(this.ProcessTablixCell(c));
                        }
                    }
                }
            }

            tablixObj.TablixBody = tablixBody;

            return tablixObj;
        }

        private TablixCell ProcessTablixCell(XElement cell)
        {
            var tablixCell = new TablixCell
            {
                TablixCellContent = new List<TablixCellContent>()
            };            
            var cellContents = cell.Elements(_ns1 + "CellContents");

            if (cellContents != null)
            {
                foreach (var c in cellContents)
                {
                    var textboxes = c.Elements(_ns1 + "Textbox");

                    if (textboxes != null)
                    {
                        foreach (var textbox in textboxes)
                        {
                            tablixCell.TablixCellContent.Add(this.ProcessTextbox(textbox));
                        }                        
                    }

                    // Process other types.
                }
            }

            return tablixCell;
        }

        private Textbox ProcessTextbox(XElement textbox)
        {
            var textboxObj = new Textbox
            {
                Name = textbox.Attribute("Name")?.Value,
                CanGrow = textbox.Element(_ns1 + "CanGrow")?.Value == "true",
                KeepTogether = textbox.Element(_ns1 + "KeepTogether")?.Value == "true"
            };

            var paragraphs = textbox.Elements(_ns1 + "Paragraphs").Elements(_ns1 + "Paragraph");
            var style = textbox.Element(_ns1 + "Style");

            if (paragraphs != null)
            {
                textboxObj.Paragraphs = new List<Paragraph>();

                foreach (var p in paragraphs)
                {
                    var paragraphObj = new Paragraph();

                    var textRuns = p.Elements(_ns1 + "TextRuns").Elements(_ns1 + "TextRun");

                    if (textRuns != null)
                    {
                        paragraphObj.TextRuns = new List<TextRun>();

                        foreach (var tr in textRuns)
                        {
                            var textRunObj = new TextRun
                            {
                                Value = tr.Element(_ns1 + "Value")?.Value,
                                Style = this.ProcessStyle(tr.Element(_ns1 + "Style"))
                            };
                                                        
                            paragraphObj.TextRuns.Add(textRunObj);
                        }                        
                    }

                    textboxObj.Paragraphs.Add(paragraphObj);
                }
            }

            textboxObj.Style = this.ProcessStyle(style);
            textboxObj.Style.Top = textbox.Element(_ns1 + "Top")?.Value;
            textboxObj.Style.Left = textbox.Element(_ns1 + "Left")?.Value;
            textboxObj.Style.Height = textbox.Element(_ns1 + "Height")?.Value;
            textboxObj.Style.Width = textbox.Element(_ns1 + "Width")?.Value;
            textboxObj.Style.ZIndex = textbox.Element(_ns1 + "ZIndex")?.Value;

            return textboxObj;
        }

        private Style ProcessStyle(XElement style)
        {
            if (style == null)
            {
                return new Style();
            }

            return new Style(style, _ns1);
        }

        
    }
}
