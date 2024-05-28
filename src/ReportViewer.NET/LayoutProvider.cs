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
using System.Data.Common;

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

                    reportParam.DataSetReference.DataSetResults = (await this.RunDataSetQuery(reportParam.DataSetReference, reportParams, userProvidedParameters)).ToList();

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

            // TODO: IF OK, CREATE RUN REPORT BUTTON.
            if (!invalidParameter)
            {
                sb.AppendLine(@"<button type=""button"" id=""RunReportBtn"">Run report</button>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"reportoutput-container\"></div>");
            return new HtmlString(sb.ToString());
        }
                
        public async Task<HtmlString> PublishReportOutput(IEnumerable<ReportParameter> userProvidedParameters)
        {
            var reportItems = _report.ReportItems;
            var sb = new StringBuilder();
            var invalidParameter = false;

            foreach (var reportItem in reportItems) 
            {
                if (reportItem is Tablix)
                {
                    var tablix = (Tablix)reportItem;

                    if (tablix.DataSetReference == null)
                    {
                        sb.AppendLine(tablix.Build());
                    }
                    else
                    {
                        // We potentially have calculated text which needs resolving from the Data Set.
                        var tablixText = tablix.Build();
                        tablix.DataSetReference.DataSetResults = (await this.RunDataSetQuery(tablix.DataSetReference, _report.ReportParameters, userProvidedParameters)).ToList();

                        while (tablixText.IndexOf("=Fields!") > -1)
                        {

                        }

                        sb.AppendLine(tablix.Build());
                    }
                }                
            }

            return new HtmlString(sb.ToString());
        }

        private void HandleDynamicParameterInsert(DynamicParameters param, ReportParameter rdlParameter, ReportParameter userProvidedParameter)
        {
            if (userProvidedParameter != null && rdlParameter.MultiValue && userProvidedParameter.Values != null && userProvidedParameter.Values.Count > 0)
            {
                param.Add(rdlParameter.Name, string.Join(',', userProvidedParameter.Values), DbType.String);
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

        private async Task<IEnumerable<dynamic>> RunDataSetQuery(DataSetReference dataSetReference, IEnumerable<ReportParameter> reportParams, IEnumerable<ReportParameter> userProvidedParameters)
        {
            DataSetQuery dsQuery = dataSetReference.DataSet?.Query;
            IEnumerable<dynamic> results = Enumerable.Empty<dynamic>();
            
            bool invalidParameter = false;

            if (dataSetReference.DataSet?.Query.QueryParameters != null && dataSetReference.DataSet.Query.QueryParameters.Count > 0)
            {
                var dynamicParams = new DynamicParameters();

                foreach (var queryParam in dataSetReference.DataSet.Query.QueryParameters)
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

                    this.HandleDynamicParameterInsert(dynamicParams, reportParamForDataset, userParam);
                }

                if (invalidParameter)
                {
                    // TODO: Report this to the user.
                    return results;
                }

                // Run query and use field parameters.
                var connString = _report.DataSources.FirstOrDefault(ds => ds.Name == dsQuery.DataSourceName)?.ConnectionString;

                if (!string.IsNullOrEmpty(connString))
                {
                    using (var conn = new SqlConnection(connString))
                    {
                        results = await conn.QueryAsync(dsQuery.CommandText, dynamicParams);
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
                        results = await conn.QueryAsync(dsQuery.CommandText, null);
                    }
                    catch (Exception e)
                    {
                        var t = true;
                    }
                }
            }

            return results;
        }
    }
}
