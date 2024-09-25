using ReportViewer.NET.Comparers;
using ReportViewer.NET.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Rectangle : ReportItem
    {
        public IEnumerable<ReportItem> ReportItems { get; set; }

        internal Rectangle(XElement element, ReportRDL report, IEnumerable<DataSet> datasets, ReportItem parent) : base(element, report, parent)
        {
            var reportItems = element.Elements(report.Namespace + "ReportItems");

            // Using 'this' as parent may cause issues with aggregated grouping totals. Bear this in mind if issues occur.
            this.ReportItems = ReportItem.ParseElements(reportItems, report, datasets, null, this);
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            if (!this.Hidden || (this.Hidden && this.Report.ToggleItemRequests.Contains(this.ToggleItem)))
            {
                this.Hidden = false;
                this.Style.Hidden = false;

                sb.AppendLine($"<div class=\"report-rectangle\" {this.Style?.Build()} data-toggle=\"{this.ToggleItem}\">");

                var rectangleReportRows = new List<ReportRow>()
                {
                    new ReportRow()
                };

                foreach (var reportItem in this.ReportItems)
                {
                    this.BuildReportRows(this.Report, rectangleReportRows, reportItem);
                }

                foreach (var reportRow in rectangleReportRows)
                {
                    sb.AppendLine($"<div class=\"report-row\" style=\"max-width:{reportRow.RowWidth}mm\">");

                    foreach (var reportItem in reportRow.RowItems)
                    {
                        if (reportItem is SubReport)
                        {
                            var sr = (SubReport)reportItem;
                            var srRdl = sr.GetSubReportRDL();
                            var layoutProvider = new LayoutProvider();
                            var finalUserParamsForSubReport = new List<ReportParameter>();

                            // Retrieve all parameters for the sub report.
                            foreach (var p in srRdl.ReportParameters)
                            {
                                // Search user provided parameters first
                                if (this.Report.UserProvidedParameters.Any(rp => rp.Name == p.Name))
                                {
                                    finalUserParamsForSubReport.Add(this.Report.UserProvidedParameters.First(rp => rp.Name == p.Name));
                                }
                                // Try to retrieve the parameter from the tablix row.
                                else
                                {
                                    var expressionParser = new ExpressionParser();
                                    var dataSetResults = this.GroupedResults?.Select(r => r).ToList() ?? this.DataSetReference?.DataSet?.DataSetResults;
                                    var parsedValue = expressionParser.ParseTablixExpressionString(p.Value, dataSetResults, null, this.DataSets, this.DataSetReference?.DataSet, null);

                                    if (parsedValue != null)
                                    {
                                        finalUserParamsForSubReport.Add(new ReportParameter
                                        {
                                            Name = p.Name,
                                            DataType = p.DataType,
                                            Value = Convert.ToString(parsedValue)
                                        });
                                    }
                                }
                            }

                            if (finalUserParamsForSubReport.Count != srRdl.ReportParameters.Count)
                            {
                                continue;
                            }

                            sb.AppendLine(layoutProvider.PublishReportOutput(srRdl, finalUserParamsForSubReport, this.Report.ToggleItemRequests).GetAwaiter().GetResult().Value);
                        }
                        else
                        {
                            sb.AppendLine(reportItem.Build(this));
                        }
                    }

                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</div>");
            }
                                    
            return sb.ToString();
        }

        private void BuildReportRows(ReportRDL report, List<ReportRow> reportRows, ReportItem reportItem)
        {
            var currentRow = reportRows.Last();

            if (reportItem is Rectangle)
            {
                var rect = (Rectangle)reportItem;

                if (rect.ReportItems.Any())
                {
                    rect.ReportItems = rect.ReportItems.Order(new ReportItemComparer()).ToList();
                }
            }

            if (currentRow.RowHeight == 0)
            {
                currentRow.RowItems.Add(reportItem);
                reportItem.ReportRow = currentRow;

                currentRow.RowWidth = reportItem.Width + reportItem.Left;
                currentRow.RowHeight = reportItem.Height + reportItem.Top;
            }
            else
            {
                if (reportItem.Top > currentRow.RowHeight)
                {
                    var newRow = new ReportRow()
                    {
                        RowWidth = reportItem.Width + reportItem.Left,
                        RowHeight = reportItem.Height + reportItem.Top,
                    };

                    newRow.RowItems.Add(reportItem);
                    reportRows.Add(newRow);
                    reportItem.ReportRow = newRow;
                }
                else
                {
                    if (reportItem.Width + reportItem.Left > currentRow.RowWidth)
                    {
                        currentRow.RowWidth = reportItem.Width + reportItem.Left;
                    }

                    if (reportItem.Height + reportItem.Top > currentRow.RowHeight)
                    {
                        currentRow.RowHeight = reportItem.Height + reportItem.Top;
                    }

                    currentRow.RowItems.Add(reportItem);
                    reportItem.ReportRow = currentRow;
                }
            }                     
        }
    }
}
