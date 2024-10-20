﻿using ReportViewer.NET.DataObjects;
using ReportViewer.NET.DataObjects.ReportItems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers
{
    public abstract class BaseParser
    {        
        protected string CurrentString { get; private set; }
        protected TablixOperator Operator { get; private set; }
        protected TablixExpression CurrentExpression { get; private set; }
        protected IEnumerable<IDictionary<string, object>> DataSetResults { get; private set; }
        protected IDictionary<string, object> Values { get; private set; }
        protected int CurrentRowNumber { get; private set; }
        protected IEnumerable<DataSet> DataSets { get; private set; }
        protected DataSet ActiveDataset { get; private set; }        
        protected Match RegexMatch { get; private set; }
        protected ReportRDL Report { get; private set; }
        
        private int _endIndx;


        public BaseParser(
            string currentString, 
            TablixOperator op, 
            TablixExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets, 
            DataSet activeDataset,
            Regex regex,
            ReportRDL report            
        )
        {
            this.CurrentString = currentString;
            this.Operator = op;
            this.CurrentExpression = currentExpression;
            this.DataSetResults = dataSetResults;
            this.Values = values;
            this.CurrentRowNumber = currentRowNumber;
            this.DataSets = dataSets;
            this.ActiveDataset = activeDataset;            
            this.RegexMatch = regex.Match(currentString);
            this.Report = report;

            var group = this.RegexMatch.Groups[0];
            var idx = group.Index;

            currentExpression.Index = idx;
            currentExpression.Operator = op;
            
            _endIndx = idx + group.Value.Length;
        }

        /// <summary>
        /// Performs parsing of the expression. Override and add logic to decompose expression, ensuring any nested expressions are also parsed.
        /// </summary>
        public abstract void Parse();

        /// <summary>
        /// Intended use is to retrieve expression data from a pre-loaded dataset or current results for Tablix.
        /// </summary>
        /// <param name="fieldName">The field you want to retrieve from dataset.</param>
        /// <param name="dataSetName">The dataset's name, if provided by expression; otherwise null.</param>
        /// <returns>A tuple containing the type and value of the requested field.</returns>
        public abstract (Type, object) ExtractExpressionValue(string fieldName, string dataSetName);

        /// <summary>
        /// Helper method to return remaining text following the expression we intend to parse.
        /// </summary>
        /// <returns>Remaining text following the expression we intend to parse</returns>
        public string GetProposedString()
        {
            return _endIndx == this.CurrentString.Length ? "" : this.CurrentString.Substring(_endIndx, this.CurrentString.Length - _endIndx).TrimStart();
        }
    }
}
