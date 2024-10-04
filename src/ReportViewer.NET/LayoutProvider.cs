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
        public LayoutProvider() 
        {            
        }

        public async Task<HtmlString> PublishReportParameters(ReportRDL report, IEnumerable<ReportParameter> userProvidedParameters)
        {            
            var reportParams = report.ReportParameters;
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

                    reportParam.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(report, reportParam.DataSetReference.DataSet, reportParams, userProvidedParameters)).ToList();

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
                    if (reportParam.DataType != "Boolean")
                    {
                        invalidParameter = true;
                    }
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
                
        public async Task<HtmlString> PublishReportOutput(ReportRDL report, IEnumerable<ReportParameter> userProvidedParameters, IEnumerable<string> toggleItemRequests, IEnumerable<ReportMetadata> metadata)
        {
            report.UserProvidedParameters = userProvidedParameters.ToList();
            report.ToggleItemRequests = toggleItemRequests.ToList();
            report.Metadata = metadata.ToList();

            var reportBodyItems = report.ReportBodyItems;
            var reportFooterItems = report.ReportFooterItems;
            var sb = new StringBuilder();
            var bodyReportRows = new List<ReportRow>()
            {
                new ReportRow()
            };
            var footerReportRows = new List<ReportRow>()
            {
                new ReportRow()
            };

            reportBodyItems = reportBodyItems.Order(new ReportItemComparer()).ToList();
            reportFooterItems = reportFooterItems.Order(new ReportItemComparer()).ToList();

            // Build rows for body items.
            foreach (var reportItem in reportBodyItems)
            {
                await this.BuildReportRows(report, bodyReportRows, reportItem, userProvidedParameters);
            }

            // Build rows for footer items.
            foreach (var reportItem in reportFooterItems)
            {
                await this.BuildReportRows(report, footerReportRows, reportItem, userProvidedParameters);
            }

            // Process rows into HTML.
            foreach (var reportRow in bodyReportRows) 
            {
                sb.AppendLine($"<div class=\"report-row\" style=\"max-width:{reportRow.RowWidth}mm\">");

                foreach (var reportItem in reportRow.RowItems)
                {
                    if (reportItem is SubReport)
                    {
                        // Recursively build the report output for subreport using provided parameters for parent. The parent should be provided with all parameters the child needs.
                        // The subreport should also be registered with the ReportViewer and any relevant data sources registered.
                        // The subreport should be registered before the parent.
                        var sr = (SubReport)reportItem;
                        sb.AppendLine("<div class=\"sub-report-start\">");
                        sb.AppendLine((await this.PublishReportOutput(sr.GetSubReportRDL(), userProvidedParameters, toggleItemRequests, metadata)).ToString());
                        sb.AppendLine("</div>");
                    }
                    else
                    {
                        sb.AppendLine(reportItem.Build(null));
                    }                    
                }
                
                sb.AppendLine("</div>");
            }

            foreach (var reportRow in footerReportRows)
            {
                sb.AppendLine("<div class=\"report-row\">");

                foreach (var reportItem in reportRow.RowItems)
                {                    
                    sb.AppendLine(reportItem.Build(null));                    
                }

                sb.AppendLine("</div>");
            }

            var html = new HtmlString(sb.ToString());

            return new HtmlString(sb.ToString());
        }

        private async Task BuildReportRows(ReportRDL report, List<ReportRow> reportRows, ReportItem reportItem, IEnumerable<ReportParameter> userProvidedParameters)
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
                if (reportItem.Top >= currentRow.RowHeight)
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

            await this.ProcessDataSetResultsForRows(report, reportItem, userProvidedParameters);                        
        }

        private async Task ProcessDataSetResultsForRows(ReportRDL report, ReportItem reportItem, IEnumerable<ReportParameter> userProvidedParameters)
        {
            if (reportItem is Tablix)
            {
                var tablix = (Tablix)reportItem;

                if (tablix.DataSetReference != null)
                {
                    // We potentially have calculated text which needs resolving from the Data Set.
                    tablix.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(report, tablix.DataSetReference?.DataSet, report.ReportParameters, userProvidedParameters)).ToList();
                }
            }

            if (reportItem is Textbox)
            {
                var textbox = (Textbox)reportItem;
                var expression = textbox.Paragraphs.SelectMany(p => p.TextRuns).Where(tr => !string.IsNullOrEmpty(tr.Value) && tr.Value.Contains("Fields!")).Select(tr => tr.Value).FirstOrDefault();

                if (!string.IsNullOrEmpty(expression))
                {
                    var fieldParser = new FieldParser(expression, TablixOperator.Field, new TablixExpression(), null, null, textbox.DataSets, null, report);
                    var dsName = fieldParser.ExtractDataSetName();
                    var ds = report.DataSets.Where(ds => ds.Name == dsName).FirstOrDefault();

                    if (!string.IsNullOrEmpty(dsName) && ds != null && ds.DataSetResults == null)
                    {
                        ds.DataSetResults = (await this.RunDataSetQuery(report, ds, report.ReportParameters, userProvidedParameters)).ToList();
                    }
                }
            }

            if (reportItem is Rectangle)
            {
                var rect = (Rectangle)reportItem;

                foreach (var child in rect.ReportItems)
                {
                    await this.ProcessDataSetResultsForRows(report, child, userProvidedParameters);
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
                case "Integer":
                    param.Add($"@{name.TrimStart('@')}", int.Parse(value), DbType.Int32);
                    break;
            }
        }

        private async Task<IEnumerable<IDictionary<string, object>>> RunDataSetQuery(ReportRDL report, DataObjects.DataSet dataSet, IEnumerable<ReportParameter> reportParams, IEnumerable<ReportParameter> userProvidedParameters)
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
                    var reportParamForDataset = reportParams.FirstOrDefault(
                        p => p.Name == queryParam.Name.TrimStart('@') || 
                        (queryParam.Value.StartsWith("=Parameters!") && p.Name == this.ExtractParameterNameFromDataSetQueryParameter(queryParam.Value)));

                    var nullableOrDefault = reportParamForDataset != null && (reportParamForDataset.Nullable || !string.IsNullOrEmpty(reportParamForDataset.DefaultValue));
                    invalidParameter = userProvidedParameters == null || !userProvidedParameters.Any(
                        p => p.Name == queryParam.Name.TrimStart('@') ||
                        (queryParam.Value.StartsWith("=Parameters!") && p.Name == this.ExtractParameterNameFromDataSetQueryParameter(queryParam.Value))
                    );
                    var userParam = userProvidedParameters?.FirstOrDefault(
                        p => p.Name == queryParam.Name.TrimStart('@') ||
                        (queryParam.Value.StartsWith("=Parameters!") && p.Name == this.ExtractParameterNameFromDataSetQueryParameter(queryParam.Value))
                    );

                    if (invalidParameter || (!nullableOrDefault && string.IsNullOrEmpty(userParam.Value) && !userParam.Values.Any()))
                    {
                        break;
                    }

                    // Overwrite name from QueryParameter if needed. They might not be the same.
                    reportParamForDataset.Name = queryParam.Name.TrimStart('@');
                                        
                    this.HandleDynamicParameterInsert(dynamicParams, reportParamForDataset, userParam, dsQuery);
                }

                if (invalidParameter)
                {
                    // TODO: Report this to the user.
                    return this.TransformDapperKeys(results);
                }

                // Run query and use field parameters.
                var connString = report.DataSources.FirstOrDefault(
                    ds => ds.Name == dsQuery.DataSourceName ||
                    (!string.IsNullOrEmpty(ds.DataSourceReference) && !string.IsNullOrEmpty(dsQuery.DataSourceReference) && ds.DataSourceReference == dsQuery.DataSourceReference)
                )?.ConnectionString;

                if (!string.IsNullOrEmpty(connString))
                {
                    using (var conn = new SqlConnection(connString))
                    {
                        results = await conn.QueryAsync<dynamic>(dsQuery.CommandText, dynamicParams, commandType: dsQuery.CommandType == "StoredProcedure" ? CommandType.StoredProcedure : null);
                    }
                }
            }
            else
            {
                // We can run the query as no user fields are required.
                var connString = report.DataSources.FirstOrDefault(
                    ds => ds.Name == dsQuery.DataSourceName || 
                    (!string.IsNullOrEmpty(ds.DataSourceReference) && !string.IsNullOrEmpty(dsQuery.DataSourceReference) && ds.DataSourceReference == dsQuery.DataSourceReference)
                )?.ConnectionString;

                using (var conn = new SqlConnection(connString))
                {
                    try
                    {
                        results = await conn.QueryAsync<dynamic>(dsQuery.CommandText, null, commandType: dsQuery.CommandType == "StoredProcedure" ? CommandType.StoredProcedure : null);
                    }
                    catch (Exception e)
                    {                        
                    }
                }
            }

            return this.TransformDapperKeys(results);
        }

        private string ExtractParameterNameFromDataSetQueryParameter(string parameterName)
        {
            if (!parameterName.StartsWith("=Parameters!"))
            {
                return string.Empty;
            }

            // This probably isn't robust enough so look into a better solution if not appropriate.
            return parameterName.Replace("=Parameters!", "").Replace(".Value", "");
        }

        private List<IDictionary<string, object>> TransformDapperKeys(IEnumerable<dynamic> results)
        {
            var dicList = new List<IDictionary<string, object>>();

            foreach (IDictionary<string, object> dic in results)
            {
                var dict = new Dictionary<string, object>();

                foreach (KeyValuePair<string, object> kvp in dic)
                {
                    dict.Add(kvp.Key.Replace(' ', '_').ToLower(), kvp.Value);
                }

                dicList.Add(dict);
            }

            return dicList;
        }
    }
}
