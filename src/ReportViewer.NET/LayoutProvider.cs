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
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ReportViewer.NET
{
    internal class LayoutProvider
    {
        public static Regex CountRegex = new Regex("(?:\\(*?)(?:Count?)(\\((.*?)\\)\\)*)");
        public static Regex FieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

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

                    reportParam.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(reportParam.DataSetReference, reportParams, userProvidedParameters)).ToList();

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

            // If OK, create run report button.
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

            reportItems = reportItems.OrderBy(ri => ri.Top).ThenBy(ri => ri.Left).ToList();

            foreach (var reportItem in reportItems)
            {
                if (reportItem is Tablix)
                {
                    var tablix = (Tablix)reportItem;

                    if (tablix.DataSetReference != null)
                    {
                        // We potentially have calculated text which needs resolving from the Data Set.
                        tablix.DataSetReference.DataSet.DataSetResults = (await this.RunDataSetQuery(tablix.DataSetReference, _report.ReportParameters, userProvidedParameters)).ToList();
                    }
                }
            }

            foreach (var reportItem in reportItems) 
            {
                sb.AppendLine(reportItem.Build());
            }

            return new HtmlString(sb.ToString());
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

                    this.HandleDynamicParameterInsert(dynamicParams, reportParamForDataset, userParam, dsQuery);
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

        private string ParseTablixExpressionString(string tablixText, DataSetReference dataSetReference, IEnumerable<DataObjects.DataSet> dataSets)
        {            
            string currentString = tablixText;
            List<TablixExpression> expressions = new List<TablixExpression>();

            // TODO: Parse built in expressions, e.g. Globals.

            while (!string.IsNullOrEmpty(currentString))
            {
                var currentExpression = new TablixExpression();
                var proposedString = string.Empty;
                                
                if (CountRegex.IsMatch(currentString) && 
                    (currentExpression.Operator == TablixOperator.None || CountRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var regMatch = CountRegex.Match(currentString);
                    var group = regMatch.Groups[0];
                    var idx = group.Index;
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Count;

                    var endIndx = idx + group.Value.Length;

                    this.ParseCountExpression(currentString, currentExpression, dataSetReference, dataSets);
                                        
                    proposedString = (endIndx == currentString.Length) ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
                }

                if (currentString.IndexOf("+") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("+") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("+");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Add;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("-") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("-") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("-");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Subtract;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("*") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("*") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("*");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Multiply;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (currentString.IndexOf("/") > -1 &&
                    (currentExpression.Operator == TablixOperator.None || currentString.IndexOf("/") < currentExpression.Index)
                )
                {
                    var idx = currentString.IndexOf("/");
                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Divide;

                    proposedString = currentString.Substring(idx + 1, currentString.Length - idx - 1);
                }

                if (FieldRegex.IsMatch(currentString) &&
                    (currentExpression.Operator == TablixOperator.None || FieldRegex.Match(currentString).Index < currentExpression.Index)
                )
                {
                    var regMatch = FieldRegex.Match(currentString);
                    var group = regMatch.Groups[0];
                    var idx = group.Index;

                    currentExpression.Index = idx;
                    currentExpression.Operator = TablixOperator.Field;

                    var endIndx = idx + group.Value.Length;

                    this.ParseFieldExpression(currentString, currentExpression, dataSetReference, dataSets);

                    proposedString = (endIndx == currentString.Length) ? "" : currentString.Substring(endIndx, currentString.Length - endIndx);
                }

                if (currentExpression.Operator == TablixOperator.None)
                {
                    break;
                }

                expressions.Add(currentExpression);
                currentString = proposedString;
            }

            return this.ParseTablixExpression(expressions);
        }

        private string ParseTablixExpression(IEnumerable<TablixExpression> expressions)
        {
            TablixOperator lastOperator;
            
            if (expressions.Count() > 0)
            {
                var final = expressions.Aggregate((prev, next) =>
                {
                    var newExpr = new TablixExpression();

                    switch (next.Operator)
                    {
                        case TablixOperator.Count:
                            newExpr.Value = (int)prev.Value;
                            break;
                        case TablixOperator.Add:
                            newExpr.Value = (int)prev.Value + (int)next.Value;
                            break;
                        case TablixOperator.Subtract:
                            newExpr.Value = (int)prev.Value - (int)next.Value;
                            break;
                        case TablixOperator.Multiply:
                            newExpr.Value = (int)prev.Value * (int)next.Value;
                            break;
                        case TablixOperator.Divide:
                            newExpr.Value = (int)prev.Value / (int)next.Value;
                            break;
                    }

                    return newExpr;
                });

                return final.Value.ToString();
            }

            return string.Empty;
        }

        private void ParseCountExpression(string currentString, TablixExpression expression, DataSetReference dataSetReference, IEnumerable<DataObjects.DataSet> dataSets)
        {
            var fieldRegex = new Regex("(\\bFields!\\b[^\\)]+)");

            // TODO: Handle other count expressions not using fields??
            if (fieldRegex.IsMatch(currentString))
            {
                var match = fieldRegex.Match(currentString);
                var matchString = match.Value;

                var fieldsIdx = matchString.IndexOf("Fields!");
                var fieldEnd = matchString.IndexOf(".", fieldsIdx);
                var fieldName = matchString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

                expression.Index = match.Index;
                expression.Field = fieldName;

                var dataSetStart = matchString.IndexOf('"', fieldEnd);
                var dataSetEnd = matchString.IndexOf('"', dataSetStart + 1); // Add 1 so we don't find the same quote as dataSetStart.
                var dataSetName = matchString.Substring(dataSetStart + 1, dataSetEnd - dataSetStart - 1);

                expression.DataSetName = dataSetName;
                expression.Value = this.ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetReference, dataSets);                                
            }                        
        }

        private void ParseFieldExpression(string currentString, TablixExpression expression, DataSetReference dataSetReference, IEnumerable<DataObjects.DataSet> dataSets)
        {
            var fieldsIdx = currentString.IndexOf("Fields!");
            var fieldEnd = currentString.IndexOf(".", fieldsIdx);
            var fieldName = currentString.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7));

            expression.Index = fieldsIdx;
            expression.Field = fieldName;
            expression.Value = this.ExtractExpressionValue(expression.DataSetName, fieldName, expression.Operator, dataSetReference, dataSets);
        }

        private dynamic ExtractExpressionValue(string dataSetName, string fieldName, TablixOperator op, DataSetReference dataSetReference, IEnumerable<DataObjects.DataSet> dataSets)
        {
            if (dataSetReference != null && dataSetReference.DataSetName == dataSetName)
            {
                var dataSetResults = dataSetReference.DataSet?.DataSetResults;

                if (dataSetResults != null)
                {
                    switch (op)
                    {
                        case TablixOperator.Count:
                            return dataSetResults.Count;                            
                        case TablixOperator.Field:
                            foreach (IDictionary<string, object> expando in dataSetResults)
                            {
                                if (expando.ContainsKey(fieldName))
                                {
                                    return expando[fieldName];
                                }
                            }
                            break;
                    }                                        
                }
            }
            else
            {
                var dataSet = dataSets.FirstOrDefault(ds => ds.Name == dataSetName);

                if (dataSet != null)
                {
                    var dataSetResults = dataSet.DataSetResults;

                    if (dataSetResults != null)
                    {
                        switch (op)
                        {
                            case TablixOperator.Count:
                                return dataSetResults.Count;
                            case TablixOperator.Field:
                                foreach (IDictionary<string, object> expando in dataSetResults)
                                {
                                    if (expando.ContainsKey(fieldName))
                                    {
                                        return expando[fieldName];
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return null;
        }
    }
}
