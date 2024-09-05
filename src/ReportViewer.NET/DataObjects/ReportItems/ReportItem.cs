﻿using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{    
    public abstract class ReportItem
    {        
        public ReportRDL Report { get; set; }
        public string Name { get; set; }        
        public Style Style { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }        
        public ReportRow ReportRow { get; set; }
        public IGrouping<object, IDictionary<string, object>> GroupedResults { get; set; }
        public string DataSetName { get; set; }
        public DataSetReference DataSetReference { get; set; }
        public IEnumerable<DataSet> DataSets { get; set; }

        public ReportItem(XElement element, ReportRDL report)
        {
            this.Report = report;

            var topValue = Style.ConvertUnit(element.Element(report.Namespace + "Top")?.Value);
            var leftValue = Style.ConvertUnit(element.Element(report.Namespace + "Left")?.Value);
            var widthValue = Style.ConvertUnit(element.Element(report.Namespace + "Width")?.Value);
            var heightValue = Style.ConvertUnit(element.Element(report.Namespace + "Height")?.Value);

            if (!string.IsNullOrEmpty(topValue) && double.TryParse(topValue.Substring(0, topValue.Length - 2), out var top))
            {
                Top = top;
            }

            if (!string.IsNullOrEmpty(leftValue) && double.TryParse(leftValue.Substring(0, leftValue.Length - 2), out var left))
            {
                Left = left;
            }

            if (!string.IsNullOrEmpty(widthValue) && double.TryParse(widthValue.Substring(0, widthValue.Length - 2), out var width))
            {
                Width = width;
            }

            if (!string.IsNullOrEmpty(heightValue) && double.TryParse(heightValue.Substring(0, heightValue.Length - 2), out var height))
            {
                Height = height;
            }

            this.Style = new Style(element.Element(report.Namespace + "Style"), report);
            this.Style.Top = topValue;
            this.Style.Left = leftValue;
            this.Style.Height = heightValue;
            this.Style.Width = widthValue;

            this.Hidden = this.Style.Hidden = element.Element(report.Namespace + "Visibility")?.Element(report.Namespace + "Hidden")?.Value == "true";
            this.ToggleItem = element.Element(report.Namespace + "Visibility")?.Element(report.Namespace + "ToggleItem")?.Value;

            if (this.Hidden)
            {
                this.Report.HiddenItems.Add(this);
            }            
        }

        internal static IEnumerable<ReportItem> ParseElements(
            IEnumerable<XElement> elements, 
            ReportRDL report, 
            IEnumerable<DataSet> datasets,
            TablixCell cell
        )
        {
            var reportItems = new List<ReportItem>();

            if (elements != null)
            {
                foreach (XElement ri in elements)
                {
                    var textboxes = ri.Elements(report.Namespace + "Textbox");

                    if (textboxes != null)
                    {
                        foreach (var textbox in textboxes)
                        {
                            reportItems.Add(new Textbox(cell, textbox, datasets, report));
                        }
                    }

                    // Process other types.
                    var subreports = ri.Elements(report.Namespace + "Subreport");

                    if (subreports != null)
                    {
                        foreach (var sr in subreports)
                        {
                            var srPath = sr.Element(report.Namespace + "ReportName")?.Value;
                            var srName = srPath.Split('/').Last();
                            var registeredReport = report.CurrentRegisteredReports.First(r => r.Name == srName);
                            var subReportParameters = sr.Element(report.Namespace + "Parameters").Elements(report.Namespace + "Parameter");

                            // Append the parameter expression values to the registered report as these won't have been added during registration.
                            foreach (var subReportParam in subReportParameters)
                            {
                                var paramName = subReportParam.Attribute("Name")?.Value;
                                var registeredParam = registeredReport.ReportParameters.First(p => p.Name == paramName);
                                registeredParam.Value = subReportParam.Value;
                            }

                            reportItems.Add(new SubReport(sr, report, report.CurrentRegisteredReports.First(r => r.Name == srName)));
                        }
                    }

                    var tablixs = ri.Elements(report.Namespace + "Tablix");

                    if (tablixs != null)
                    {
                        foreach (var tablix in tablixs)
                        {
                            reportItems.Add(new Tablix(tablix, datasets, report));
                        }
                    }

                    var rectangles = ri.Elements(report.Namespace + "Rectangle");

                    if (rectangles != null)
                    {
                        foreach (var r in rectangles)
                        {
                            reportItems.Add(new Rectangle(r, report, datasets));
                        }
                    }
                }
            }

            return reportItems;
        }

        public void NestedCopy(ReportItem parent, ReportItem child)
        {
            // Is this ReportItem object nested within a Tablix group? If so, we want it to have visibility of data grouping that's been carried out by the outer hierarchy.
            if (parent == null)
            {
                return;
            }
                
            child.DataSetName = parent.DataSetName;
            child.DataSetReference = parent.DataSetReference;
            child.DataSets = parent.DataSets;
            child.GroupedResults = parent.GroupedResults;
        }

        public abstract string Build(ReportItem parent);
    }

    public class ReportRow
    {        
        public double RowWidth { get; set; }
        public double RowHeight { get; set; }
        public List<ReportItem> RowItems { get; set; } = new List<ReportItem>();
    }

    public enum PageBreak
    {
        None,
        Start,
        End,
        StartAndEnd,
        Between        
    }
}
