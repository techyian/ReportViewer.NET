﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportViewer.NET.Parsers
{
    public class ReportExpression
    {
        public int Index { get; set; } = -1;
        public int EndIndex { get; set; }
        public ExpressionFieldOperator Operator { get; set; }
        public string Field { get; set; }
        public Type ResolvedType { get; set; }
        public object Value { get; set; }
        public string DataSetName { get; set; }
    }

    public enum ExpressionFieldOperator
    {
        None,
        Count,
        CountDistinct,
        Sum,
        Field,
        Parameter,
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod,
        String,
        If,
        IsArray,
        IsDate,
        IsNothing,
        IsNumeric,
        Equals,
        LessThan,
        LessThanEqualTo,
        GreaterThan,
        GreaterThanEqualTo,
        NotEqual,
        Like,
        Is,
        ConcatAnd,
        ConcatPlus,
        ExecutionTime,
        Language,
        And,
        Not,
        Or,
        Xor,
        AndAlso,
        OrElse,
        Left,
        MonthName,
        FormatCurrency,
        RowNumber
    }
}