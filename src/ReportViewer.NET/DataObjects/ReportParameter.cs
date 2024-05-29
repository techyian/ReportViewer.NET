using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReportViewer.NET.DataObjects
{
    public class ReportParameter
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public Type TypeName { get; set; }
        public string Prompt { get; set; }
        public bool MultiValue { get; set; }
        public bool Nullable { get; set; }
        public string Value { get; set; }
        public List<string> Values { get; set; }
        public string DefaultValue { get; set; }
        public DataSetReference DataSetReference { get; set; }

        public string Build(ReportParameter userProvidedParameter)
        {
            if (!string.IsNullOrEmpty(this.DataType))
            {
                var nullable = this.Nullable ? "true" : "false";

                if (this.DataSetReference != null && this.DataSetReference.DataSet.DataSetResults != null)
                {
                    var idx = 0;
                    var multiValue = this.MultiValue ? "true" : "false";

                    var sb = new StringBuilder();
                    sb.AppendLine("<div class=\"reportparam reportparam-list\">");
                    sb.AppendLine("<label>" + this.Prompt + @"</label>");
                    
                    foreach (IDictionary<string, object> res in this.DataSetReference.DataSet.DataSetResults)
                    {                        
                        //var name = this.MultiValue ? this.Name + "[]" : this.Name;
                        var elementId = $"{this.Name}-{idx}";
                        
                        if (userProvidedParameter != null &&
                            (
                             (this.MultiValue && userProvidedParameter.Values != null && userProvidedParameter.Values.Any(p => p == res[this.DataSetReference.ValueField].ToString())) || 
                             (!this.MultiValue && userProvidedParameter.Value == res[this.DataSetReference.ValueField].ToString())
                            ) 
                        )
                        {
                            sb.AppendLine(@"                                
                                <div class=""custom-control custom-checkbox"">
                                    <input type=""checkbox"" class=""custom-control-input"" id=""" + elementId + @""" name=""" + this.Name + @""" value=""" + res[this.DataSetReference.ValueField] + @""" data-multivalue=""" + multiValue + @""" checked>
                                    <label class=""custom-control-label pl-3"" for=""" + elementId + @""">" + res[this.DataSetReference.LabelField] + @"</label>
                                </div>
                            ");
                        }
                        else
                        {
                            sb.AppendLine(@"                                
                                <div class=""custom-control custom-checkbox"">
                                    <input type=""checkbox"" class=""custom-control-input"" id=""" + elementId + @""" name=""" + this.Name + @""" value=""" + res[this.DataSetReference.ValueField] + @""" data-multivalue=""" + multiValue + @""">
                                    <label class=""custom-control-label pl-3"" for=""" + elementId + @""">" + res[this.DataSetReference.LabelField] + @"</label>
                                </div>
                            ");
                        }
                        
                        idx++;
                    }

                    sb.AppendLine("</div>");

                    return sb.ToString();
                }
                else
                {
                    var value = userProvidedParameter != null && !string.IsNullOrEmpty(userProvidedParameter.Value) ? userProvidedParameter.Value : "";
                    var check = userProvidedParameter != null && !string.IsNullOrEmpty(userProvidedParameter.Value) ? "checked" : "";

                    switch (this.DataType)
                    {
                        case "String":
                            return @"<div class=""reportparam reportparam-string"">
                                    <label for=""" + this.Name + @""">" + this.Prompt + @"</label>
                                    <input type=""text"" id=""" + this.Name + @""" name=""" + this.Name + @""" class=""form-control"" data-nullable=""" + nullable + @""" value=""" + value + @""" /> 
                                </div> ";
                        case "DateTime":
                            return @"
                                <div class=""reportparam reportparam-date date"">
                                        <label for=""" + this.Name + @""">" + this.Prompt + @"</label>                                        
                                        <input type=""text"" id=""" + this.Name + @""" name=""" + this.Name + @""" class=""form-control"" data-nullable=""" + nullable + @""" value=""" + value + @""" />
                                </div>
                                ";
                        case "Boolean":
                            return @"<div class=""reportparam reportparam-boolean"">
                                    <div class=""custom-control custom-checkbox"">
                                        <input type=""checkbox"" class=""custom-control-input"" id=""" + this.Name + @""" name=""" + this.Name + @""" value=""true"" " + check + @">
                                        <label class=""custom-control-label pl-3"" for=""" + this.Name + @""">" + this.Prompt + @"</label>
                                    </div>
                                </div> ";
                    }
                }                
            }

            return string.Empty;
        }
    }

    public class DataSetReference
    {
        public string DataSetName { get; set; }
        public string ValueField { get; set; }
        public string LabelField { get; set; }
        public DataSet? DataSet { get; set; }
    }
}
