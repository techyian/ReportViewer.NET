﻿using ReportViewer.NET.DataObjects;
using ReportViewer.NET.Extensions;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReportViewer.NET.Parsers.DateAndTime
{
    public class SecondParser : BaseParser
    {
        public static Regex SecondRegex = RegexCommon.GenerateMultiParamParserRegex("Second");

        public SecondParser(
            string currentString,
            ExpressionFieldOperator op,
            ReportExpression currentExpression,
            IEnumerable<IDictionary<string, object>> dataSetResults,
            IDictionary<string, object> values,
            int currentRowNumber,
            IEnumerable<DataSet> dataSets,
            DataSet activeDataset,
            ReportRDL report
        ) : base(currentString, op, currentExpression, dataSetResults, values, currentRowNumber, dataSets, activeDataset, SecondRegex, report)
        {
        }

        public override (Type, object) ExtractExpressionValue(string fieldName, string dataSetName)
        {
            throw new NotImplementedException();
        }

        public override void Parse()
        {
            var match = SecondRegex.Match(this.CurrentString);
            var matchValue = match.Value;

            // Remove the surrounding Second including open & close brace so we can inspect inner members and see if they too contain program flow expressions. 
            matchValue = matchValue.MatchValueSubString(7);

            var dtt = this.Report.Parser.ParseReportExpressionString(
                matchValue,
                this.DataSetResults,
                this.Values,
                this.CurrentRowNumber,
                this.DataSets,
                this.ActiveDataset,
                null
            ).ExpressionAsDateTime();

            this.CurrentExpression.Index = match.Index;
            this.CurrentExpression.ResolvedType = typeof(int);
            this.CurrentExpression.Value = dtt.Second;
        }

    }
}
