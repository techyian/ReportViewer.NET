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

        public int Compare(IDictionary<string, object>? xDic, IDictionary<string, object>? yDic)
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

            if (!xDic.ContainsKey(_fieldName) || xDic[_fieldName] == null)
            {
                return 1;
            }

            if (!yDic.ContainsKey(_fieldName) || yDic[_fieldName] == null)
            {
                return -1;
            }

            Type fieldType = xDic[_fieldName].GetType();

            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.String:
                    return string.Compare(xDic[_fieldName].ToString(), yDic[_fieldName].ToString());
                case TypeCode.Int32:
                    if ((int)xDic[_fieldName] < (int)yDic[_fieldName])
                    {
                        return -1;
                    }
                    if ((int)xDic[_fieldName] == (int)yDic[_fieldName])
                    {
                        return 0;
                    }
                    if ((int)xDic[_fieldName] > (int)yDic[_fieldName])
                    {
                        return 1;
                    }

                    return 0;
                case TypeCode.DateTime:
                    return ((DateTime)xDic[_fieldName]).CompareTo((DateTime)yDic[_fieldName]);
            }

            return 0;
        }
    }
}
