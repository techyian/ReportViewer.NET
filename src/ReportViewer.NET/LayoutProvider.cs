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
using System.Transactions;
using System.Xml.Linq;
using System.Data.Common;
using System.Runtime.InteropServices.JavaScript;

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

            sb.AppendLine("<div class=\"reportparameters-container\">");

            foreach (var reportParam in reportParams.DistinctBy(rp => rp.Name))
            {
                if (reportParam.DataSetReference == null)
                {
                    sb.AppendLine(reportParam.Build());
                }
                else
                {
                    // Get report parameters which require data sets to be ran.
                    // We need to ensure that the user has provided the relevant field data.
                    if (reportParam.DataSetReference.DataSet == null)
                        continue;

                    var dsQuery = reportParam.DataSetReference.DataSet.Query;
                    IEnumerable<dynamic> results = null;

                    if (reportParam.DataSetReference.DataSet.Query.QueryParameters != null && reportParam.DataSetReference.DataSet.Query.QueryParameters.Count > 0)
                    {
                        var invalidParameter = false;
                        var dynamicParams = new DynamicParameters();

                        foreach (var queryParam in reportParam.DataSetReference.DataSet.Query.QueryParameters)
                        {
                            // Find user provided value for field.
                            invalidParameter = userProvidedParameters == null || !userProvidedParameters.Any(p => p.Name == queryParam.Name.TrimStart('@'));
                                                        
                            if (invalidParameter)
                            {
                                break;
                            }

                            var userParam = userProvidedParameters.First(p => p.Name == queryParam.Name.TrimStart('@'));

                            this.HandleDynamicParameterInsert(dynamicParams, reportParam, userParam);
                        }

                        if (invalidParameter)
                        {
                            // TODO: Report this to the user.
                            continue;
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

                    reportParam.DataSetReference.DataSetResults = results.ToList();

                    sb.AppendLine(reportParam.Build());
                }
            }

            // TODO: IF OK, CREATE RUN REPORT BUTTON.

            sb.AppendLine("</div>");

            return new HtmlString(sb.ToString());
        }

        private void HandleDynamicParameterInsert(DynamicParameters param, ReportParameter rdlParameter, ReportParameter userProvidedParameter)
        {            
            if (rdlParameter.MultiValue && userProvidedParameter.Values != null && userProvidedParameter.Values.Count > 0)
            {
                param.Add(rdlParameter.Name, string.Join(',', userProvidedParameter.Values), DbType.String);
                return;
            }

            if (!string.IsNullOrEmpty(userProvidedParameter.Value))
            {
                switch (rdlParameter.DataType)
                {
                    case "String":
                        param.Add(rdlParameter.Name, userProvidedParameter.Value, DbType.String);
                        break;
                    case "DateTime":
                        param.Add(rdlParameter.Name, userProvidedParameter.Value, DbType.DateTime);                        
                        break;
                    case "Boolean":
                        param.Add(rdlParameter.Name, userProvidedParameter.Value == "True", DbType.Boolean);                        
                        break;
                }
            }                        
        }

        //public HtmlString PublishReportOutput()
        //{

        //}
    }
}
