using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public abstract class BaseParser
    {        
        protected string CurrentString { get; set; }
        protected TablixOperator Operator { get; set; }
        protected TablixExpression CurrentExpression { get; set; }
        protected IEnumerable<IDictionary<string, object>> DataSetResults { get; set; }
        protected IDictionary<string, object> Values { get; set; }
        protected IEnumerable<DataSet> DataSets { get; set; }
        protected Match RegexMatch { get; set; }

        private int _endIndx;


        public BaseParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            IEnumerable<DataSet> dataSets, 
            Regex regex
        )
        {
            this.CurrentString = currentString;
            this.Operator = op;
            this.CurrentExpression = currentExpression;
            this.DataSetResults = dataSetResults;
            this.Values = values;
            this.DataSets = dataSets;
            this.RegexMatch = regex.Match(currentString);

            var group = this.RegexMatch.Groups[0];
            var idx = group.Index;

            currentExpression.Index = idx;
            currentExpression.Operator = op;
            
            _endIndx = idx + group.Value.Length;
        }

        public abstract void Parse();

        public abstract (Type, object) ExtractExpressionValue(string fieldName, string dataSetName);

        public string GetProposedString()
        {
            return _endIndx == this.CurrentString.Length ? "" : this.CurrentString.Substring(_endIndx, this.CurrentString.Length - _endIndx);
        }
    }
}
