using ReportViewer.NET.DataObjects.ReportItems;
using System.Collections.Generic;

namespace ReportViewer.NET.Comparers
{
    public class ReportItemComparer : IComparer<ReportItem>
    {
        public int Compare(ReportItem x, ReportItem y)
        {
            if (x.Top == 0 || y.Top == 0)
            {
                return -1;
            }

            if (x.Top == y.Top && x.Left < y.Left)
            {
                return -1;
            }

            if (x.Top == y.Top && x.Left > y.Left)
            {
                return 1;
            }

            if (x.Top == y.Top)
            {
                return 0;
            }

            if (x.Top > y.Top)
            {
                if (x is Line)
                {
                    // x top is higher, line is absolute so don't consider height.
                    return 1;
                }

                if (x.Top > y.Top + y.Height && !(y is Line))
                {   
                    // New row.
                    return 1;
                }
                else if (x.Top > y.Top && y is Line)
                {                    
                    return 1;
                }
                else
                {
                    // Same row.
                    if (x.Left > y.Left)
                    {
                        return 1;
                    }

                    return -1;
                }
            }
            else
            {
                if (x is Line)
                {
                    // x top is lower, line is absolute so don't consider height.
                    return -1;
                }

                if (y.Top > x.Top + x.Height && !(x is Line))
                {                    
                    return -1;
                }
                else if (y.Top > x.Top && x is Line)
                {
                    return -1;
                }
                else
                {
                    // Same row.
                    if (y.Left > x.Left)
                    {
                        return -1;
                    }

                    return 1;
                }
            }
        }
    }
}
