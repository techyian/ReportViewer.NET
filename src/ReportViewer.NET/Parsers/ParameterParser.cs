using ReportViewer.NET.DataObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    internal class ParameterParser : BaseParser
    {
        public static Regex ParameterRegex = new Regex("(\\bParameters!\\b(.*?)\\.Value)", RegexOptions.IgnoreCase);
        
        public ParameterParser(
            string currentString, 
            ExpressionFieldOperator op, 
            ReportExpression currentExpression, 
            IEnumerable<IDictionary<string, object>> dataSetResults, 
            IDictionary<string, object> values, 
            int currentRowNumber,
            IEnumerable<DataSet> dataSets, 
            DataSet activeDataset,             
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, ParameterRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            var parameter = this.Report.UserProvidedParameters?.FirstOrDefault(p => p.Name?.ToLower() == fieldName);

            if (parameter == null)
            {
                return (typeof(object), null);
            }

            switch (parameter.DataType.ToLower())
            {
                case "string":
                    return (typeof(string), parameter.Value);
                case "boolean":
                    bool.TryParse(parameter.Value, out var bValue);
                    return (typeof(bool), bValue);
                case "integer":
                    long.TryParse(parameter.Value, out var lValue);
                    return (typeof(long), lValue);
                case "datetime":
                    DateTime.TryParse(parameter.Value, out var dttValue);
                    return (typeof(DateTime), dttValue);
                case "float":
                    double.TryParse(parameter.Value, CultureInfo.InvariantCulture, out var dValue);
                    return (typeof(double), dValue);
                //case "binary":
                //    // TODO
                //    break;
                //case "varient":
                //    // TODO
                //    break;
                //case "varientarray":
                //    // TODO
                //    break;
                //case "serializable":
                //    // TODO
                //    break;
                default:
                    return (typeof(object), parameter.Value);
            }
        }

        public override void Parse()
        {
            var paramIdx = this.CurrentString.IndexOf("Parameters!");
            var paramEnd = this.CurrentString.IndexOf(".", paramIdx);
            var paramName = this.CurrentString.Substring(paramIdx + 11, paramEnd - (paramIdx + 11)).ToLower();

            this.CurrentExpression.Index = paramIdx;
            this.CurrentExpression.Field = paramName;

            (Type, object) extractedValue = ExtractExpressionValue(paramName, this.CurrentExpression.DataSetName);

            this.CurrentExpression.ResolvedType = extractedValue.Item1;
            this.CurrentExpression.Value = extractedValue.Item2;
        }
    }
}
