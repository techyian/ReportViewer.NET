using Dapper;
using Microsoft.AspNetCore.Html;
using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReportViewer.NET.DataObjects.ReportItems;
using ReportViewer.NET.Comparers;
using ReportViewer.NET.Parsers;

namespace ReportViewer.NET
{
    internal class LayoutProvider
    {        
        private ReportRDL _report;

        public LayoutProvider(ReportRDL report) 
        {
            _report = report;
        }

        public async Task<HtmlString> PublishReportParameters(IEnumerable<ReportParameter> userProvidedParameters)
        {            
            var reportParams = _report.ReportParameters;
            var sb = new StringBuilder();
            var invalidParameter = false;

            sb.AppendLine("<div class=\"reportparameters-container\">");

            foreach (var reportParam in reportParams.DistinctBy(rp => rp.Name))
            {
                if (reportParam.DataSetReference == null)
                {
                    sb.AppendLine(reportParam.Build(userProvidedParameters?.FirstOrDefault(p => p.Name == reportParam.Name)));
                }
                else
                {
                    // Get report parameters which require data sets to be ran.
                    // We need to ensure that the user has provided the relevant field data.
                    if (reportParam.DataSetReference.DataSet == null)
                        continue;

                    reportParam.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(reportParam.DataSetReference.DataSet, reportParams, userProvidedParameters)).ToList();

                    sb.AppendLine(reportParam.Build(userProvidedParameters?.FirstOrDefault(p => p.Name == reportParam.Name)));
                }

                if (!reportParam.Nullable && 
                    string.IsNullOrEmpty(reportParam.DefaultValue) &&
                    (userProvidedParameters == null || 
                    !userProvidedParameters.Any(
                        p => p.Name == reportParam.Name.TrimEnd('@') && ((reportParam.MultiValue && (p.Values?.Count > 0)) || (!reportParam.MultiValue && !string.IsNullOrEmpty(p.Value)))
                        )
                    )
                   )
                {
                    invalidParameter = true;
                }
            }
                        
            sb.AppendLine("</div>");

            // If OK, create run report button.
            if (!invalidParameter)
            {
                sb.AppendLine("<div class=\"reportparameters-run\">");
                sb.AppendLine("<button type=\"button\" id=\"RunReportBtn\">Run report</button>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine($"<div class=\"reportoutput-container\"></div>");
            return new HtmlString(sb.ToString());
        }
                
        public async Task<HtmlString> PublishReportOutput(IEnumerable<ReportParameter> userProvidedParameters)
        {
            var reportBodyItems = _report.ReportBodyItems;
            var reportFooterItems = _report.ReportFooterItems;
            var sb = new StringBuilder();
            var reportRows = new List<ReportRow>()
            {
                new ReportRow()
            };
            
            reportBodyItems = reportBodyItems.Order(new ReportItemComparer()).ToList();
            reportFooterItems = reportFooterItems.Order(new ReportItemComparer()).ToList();

            // Build rows for body items.
            foreach (var reportItem in reportBodyItems)
            {
                await this.BuildReportRows(reportRows, reportItem, userProvidedParameters);
            }

            // Build rows for footer items.
            foreach (var reportItem in reportFooterItems)
            {
                await this.BuildReportRows(reportRows, reportItem, userProvidedParameters);
            }

            var hiddenRows = reportBodyItems.Where(r => r.Hidden).ToList();
            hiddenRows.AddRange(reportFooterItems.Where(r => r.Hidden));

            // Process rows into HTML.
            foreach (var reportRow in reportRows) 
            {
                sb.AppendLine("<div class=\"report-row\">");

                foreach (var reportItem in reportRow.RowItems)
                {
                    reportItem.DoesToggle = hiddenRows.Any(r => r.ToggleItem == reportItem.Name);

                    sb.AppendLine(reportItem.Build());
                }
                
                sb.AppendLine("</div>");
            }

            return new HtmlString(sb.ToString());
        }

        private async Task BuildReportRows(List<ReportRow> reportRows, ReportItem reportItem, IEnumerable<ReportParameter> userProvidedParameters)
        {
            var currentRow = reportRows.Last();

            if (currentRow.MaxTop == 0)
            {
                currentRow.RowItems.Add(reportItem);
                currentRow.MaxTop = reportItem.Top;
                currentRow.MaxHeight = reportItem.Height;
                currentRow.MaxLeft = reportItem.Left;
                currentRow.MaxWidth = reportItem.Width;
                currentRow.TotalHeight = reportItem.Height;
                currentRow.TotalWidth = reportItem.Width;
            }
            else
            {
                if (reportItem.Top > currentRow.MaxTop + currentRow.MaxHeight)
                {
                    var newRow = new ReportRow()
                    {
                        MaxTop = reportItem.Top,
                        MaxHeight = reportItem.Height,
                        MaxLeft = reportItem.Left,
                        MaxWidth = reportItem.Width,
                        TotalHeight = reportItem.Height,
                        TotalWidth = reportItem.Width
                    };

                    newRow.RowItems.Add(reportItem);
                    reportRows.Add(newRow);
                }
                else
                {
                    currentRow.MaxTop = reportItem.Top > currentRow.MaxTop ? reportItem.Top : currentRow.MaxTop;
                    currentRow.MaxWidth = reportItem.Width > currentRow.MaxWidth ? reportItem.Width : currentRow.MaxWidth;
                    currentRow.MaxLeft = reportItem.Left > currentRow.MaxLeft ? reportItem.Left : currentRow.MaxLeft;
                    currentRow.MaxHeight = reportItem.Height > currentRow.MaxHeight ? reportItem.Height : currentRow.MaxHeight;
                    currentRow.TotalWidth += reportItem.Width;
                    currentRow.TotalHeight += reportItem.Height;
                    currentRow.RowItems.Add(reportItem);
                }
            }

            if (reportItem is Tablix)
            {
                var tablix = (Tablix)reportItem;

                if (tablix.DataSetReference != null)
                {
                    // We potentially have calculated text which needs resolving from the Data Set.
                    tablix.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(tablix.DataSetReference?.DataSet, _report.ReportParameters, userProvidedParameters)).ToList();
                }
            }

            if (reportItem is Textbox)
            {
                var textbox = (Textbox)reportItem;
                var expression = textbox.Paragraphs.SelectMany(p => p.TextRuns).Where(tr => !string.IsNullOrEmpty(tr.Value) && tr.Value.Contains("Fields!")).Select(tr => tr.Value).FirstOrDefault();

                if (!string.IsNullOrEmpty(expression))
                {
                    var fieldParser = new FieldParser(expression, TablixOperator.Field, new TablixExpression(), null, null, textbox.DataSets);
                    var dsName = fieldParser.ExtractDataSetName();
                    var ds = _report.DataSets.Where(ds => ds.Name == dsName).FirstOrDefault();

                    if (!string.IsNullOrEmpty(dsName) && ds != null && ds.DataSetResults == null)
                    {                        
                        ds.DataSetResults = (await this.RunDataSetQuery(ds, _report.ReportParameters, userProvidedParameters)).ToList();
                    }
                }                                
            }
        }

        private void HandleDynamicParameterInsert(DynamicParameters param, ReportParameter rdlParameter, ReportParameter userProvidedParameter, DataSetQuery dsQuery)
        {
            if (userProvidedParameter != null && rdlParameter.MultiValue && userProvidedParameter.Values != null && userProvidedParameter.Values.Count > 0)
            {
                if (dsQuery.CommandText.Contains($"(@{rdlParameter.Name.TrimStart('@')})"))                
                {
                    dsQuery.CommandText = dsQuery.CommandText.Replace($"(@{rdlParameter.Name.TrimStart('@')})", $"@{rdlParameter.Name.TrimStart('@')}");
                }

                param.Add(rdlParameter.Name, userProvidedParameter.Values);
                return;
            }
            else if (userProvidedParameter != null && !string.IsNullOrEmpty(userProvidedParameter.Value))
            {
                this.HandleDynamicParameterInsert(param, rdlParameter.DataType, rdlParameter.Name, userProvidedParameter.Value);                
            }
            else if (rdlParameter != null && !string.IsNullOrEmpty(rdlParameter.DefaultValue))
            {
                this.HandleDynamicParameterInsert(param, rdlParameter.DataType, rdlParameter.Name, rdlParameter.DefaultValue);
            }
            else if (rdlParameter != null && rdlParameter.Nullable)
            {
                this.HandleDynamicParameterInsert(param, rdlParameter.DataType, rdlParameter.Name, null);
            }
        }

        private void HandleDynamicParameterInsert(DynamicParameters param, string dataType, string name, string value)
        {
            switch (dataType)
            {
                case "String":
                    param.Add($"@{name.TrimStart('@')}", value, DbType.String);
                    break;
                case "DateTime":
                    param.Add($"@{name.TrimStart('@')}", value, DbType.DateTime);
                    break;
                case "Boolean":
                    param.Add($"@{name.TrimStart('@')}", value == "True", DbType.Boolean);
                    break;
            }
        }

        private async Task<IEnumerable<IDictionary<string, object>>> RunDataSetQuery(DataObjects.DataSet dataSet, IEnumerable<ReportParameter> reportParams, IEnumerable<ReportParameter> userProvidedParameters)
        {
            DataSetQuery dsQuery = dataSet?.Query;
            IEnumerable<dynamic> results = Enumerable.Empty<dynamic>();
            
            bool invalidParameter = false;

            if (dataSet?.Query.QueryParameters != null && dataSet.Query.QueryParameters.Count > 0)
            {
                var dynamicParams = new DynamicParameters();

                foreach (var queryParam in dataSet.Query.QueryParameters)
                {
                    // Find user provided value for field.
                    var reportParamForDataset = reportParams.FirstOrDefault(p => p.Name == queryParam.Name.TrimStart('@'));
                    var nullableOrDefault = reportParamForDataset != null && (reportParamForDataset.Nullable || !string.IsNullOrEmpty(reportParamForDataset.DefaultValue));
                    invalidParameter = userProvidedParameters == null || !userProvidedParameters.Any(p => p.Name == queryParam.Name.TrimStart('@'));

                    if (invalidParameter && !nullableOrDefault)
                    {
                        break;
                    }

                    var userParam = userProvidedParameters?.FirstOrDefault(p => p.Name == queryParam.Name.TrimStart('@'));

                    this.HandleDynamicParameterInsert(dynamicParams, reportParamForDataset, userParam, dsQuery);
                }

                if (invalidParameter)
                {
                    // TODO: Report this to the user.
                    return results.Cast<IDictionary<string, object>>();
                }

                // Run query and use field parameters.
                var connString = _report.DataSources.FirstOrDefault(ds => ds.Name == dsQuery.DataSourceName)?.ConnectionString;

                if (!string.IsNullOrEmpty(connString))
                {
                    using (var conn = new SqlConnection(connString))
                    {
                        results = await conn.QueryAsync(dsQuery.CommandText, dynamicParams, commandType: dsQuery.CommandType == "StoredProcedure" ? CommandType.StoredProcedure : null);
                    }
                }
            }
            else
            {
                // We can run the query as no user fields are required.
                var connString = _report.DataSources.FirstOrDefault(ds => ds.Name == dsQuery.DataSourceName)?.ConnectionString;

                using (var conn = new SqlConnection(connString))
                {
                    try
                    {
                        results = await conn.QueryAsync(dsQuery.CommandText, null, commandType: dsQuery.CommandType == "StoredProcedure" ? CommandType.StoredProcedure : null);
                    }
                    catch (Exception e)
                    {                        
                    }
                }
            }

            return results.Cast<IDictionary<string, object>>();
        }
    }
}
