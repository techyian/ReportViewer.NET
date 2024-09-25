using System;
using System.Collections.Generic;
using System.Linq;

namespace ReportViewer.NET.DataObjects
{
    public class DataSet
    {
        public string Name { get; set; }
        public DataSetQuery Query { get; set; }
        public List<DataSetField> Fields { get; set; }
        public List<IDictionary<string,object>> DataSetResults { get; set; }        
        public List<IGrouping<object, IDictionary<string, object>>> GroupedDataSetResults { get; set; }
    }

    public class DataSetQuery
    {
        public string DataSourceName { get; set; }
        public string DataSourceReference { get; set; }
        public List<DataSetQueryParameter> QueryParameters { get; set; }
        public string CommandType { get; set; }
        public string CommandText { get; set; }
    }

    public class DataSetQueryParameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class DataSetField
    {
        public string Name { get; set; }
        public string DataField { get; set; }
        public Type? TypeName { get; set; }        
        public string Label { get; set; }
    }

   
}
