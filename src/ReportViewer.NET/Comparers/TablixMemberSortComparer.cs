using System;
using System.Collections.Generic;

namespace ReportViewer.NET.Comparers
{
    public class TablixMemberSortComparer : IComparer<IDictionary<string, object>>
    {
        private string _fieldName;
        private TablixMemberSortComparer _baseComparer;

        public TablixMemberSortComparer(string fieldName, TablixMemberSortComparer baseComparer)
        {
            _fieldName = fieldName;
            _baseComparer = baseComparer;
        }

        public int Compare(IDictionary<string, object> xDic, IDictionary<string, object> yDic)
        {
            if (_baseComparer != null)
            {
                int baseResult = _baseComparer.Compare(xDic, yDic);

                if (baseResult != 0)
                {
                    return baseResult;
                }
            }

            if (xDic == null)
                throw new ArgumentNullException(nameof(xDic));

            if (yDic == null)
                throw new ArgumentNullException(nameof(yDic));

            if (!xDic.TryGetValue(_fieldName, out var x)) 
            {
                return 1;
            }

            if (!yDic.TryGetValue(_fieldName, out var y))
            {
                return -1;
            }

            Type fieldType = x.GetType();

            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.String:
                    return string.Compare(x.ToString(), y.ToString());
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16: 
                case TypeCode.UInt32: 
                case TypeCode.UInt64:
                case TypeCode.Byte:
                case TypeCode.SByte:
                    x = long.Parse(x.ToString());
                    y = long.Parse(y.ToString());

                    if ((long)x < (long)y)
                    {
                        return -1;
                    }
                    if ((long)x == (long)y)
                    {
                        return 0;
                    }
                    if ((long)x > (long)y)
                    {
                        return 1;
                    }

                    return 0;
                case TypeCode.Decimal:
                    if ((decimal)x < (decimal)y)
                    {
                        return -1;
                    }
                    if ((decimal)x == (decimal)y)
                    {
                        return 0;
                    }
                    if ((decimal)x > (decimal)y)
                    {
                        return 1;
                    }

                    return 0;

                case TypeCode.Double:
                    if ((double)x < (double)y)
                    {
                        return -1;
                    }
                    if ((double)x == (double)y)
                    {
                        return 0;
                    }
                    if ((double)x > (double)y)
                    {
                        return 1;
                    }

                    return 0;
                case TypeCode.DateTime:
                    return ((DateTime)x).CompareTo((DateTime)y);
            }

            return 0;
        }
    }
}
