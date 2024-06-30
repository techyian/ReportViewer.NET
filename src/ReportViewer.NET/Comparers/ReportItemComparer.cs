using ReportViewer.NET.DataObjects.ReportItems;
using System.Collections.Generic;

namespace ReportViewer.NET.Comparers
{
    public class ReportItemComparer : IComparer<ReportItem>
    {
        public int Compare(ReportItem x, ReportItem y)
        {
            if (x?.Top == y?.Top && x?.Left < y?.Left)
            {
                return -1;
            }

            if (x?.Top == y?.Top && x?.Left > y?.Left)
            {
                return 1;
            }

            if (x?.Top == y?.Top)
            {
                return 0;
            }

            if (x?.Top > y?.Top)
            {
                if (x?.Top > y?.Top + y?.Height)
                {
                    // New row.
                    return 1;
                }
                else
                {
                    // Same row.
                    if (x?.Left > y?.Left)
                    {
                        return 1;
                    }

                    return -1;
                }
            }
            else
            {
                if (y?.Top > x?.Top + x?.Height)
                {
                    return -1;
                }
                else
                {
                    // Same row.
                    if (y?.Left > x?.Left)
                    {
                        return -1;
                    }

                    return 1;
                }
            }
        }
    }
}
