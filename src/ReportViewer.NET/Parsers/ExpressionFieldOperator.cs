﻿using System;

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
        public bool NestedParenthesis { get; set; }
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
        Negative,
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
        ReportName,
        And,
        Not,
        Or,
        Xor,
        AndAlso,
        OrElse,
        Left,
        Asc,
        AscW,
        Chr,
        ChrW,
        Format,
        FormatNumber,
        FormatPercent,
        GetChar,
        InStr,
        InStrRev,
        MonthName,
        CDate,
        DateAdd,
        DateDiff,
        DatePart,
        DateSerial,
        DateString,
        Day,
        Now,
        DateValue,
        DateFormat,        
        Hour,
        Minute,
        Month,
        Second,
        TimeOfDay,
        Timer,
        TimeSerial,
        TimeString,
        TimeValue,
        Today,
        Weekday,
        WeekdayName,
        Year,
        FormatCurrency,
        RowNumber,
        CBool,
        CChar,
        CDec,
        CInt,        
        Round
    }
}
