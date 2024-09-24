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
                case TypeCode.Int32:
                    if ((int)x < (int)y)
                    {
                        return -1;
                    }
                    if ((int)x == (int)y)
                    {
                        return 0;
                    }
                    if ((int)x > (int)y)
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
