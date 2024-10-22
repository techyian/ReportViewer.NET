using ReportViewer.NET.DataObjects.ReportItems;
using System.Collections.Generic;

namespace ReportViewer.NET.Comparers
{
    public class ReportItemComparer : IComparer<ReportItem>
    {
        public int Compare(ReportItem x, ReportItem y)
        {
            //if (x.Top == 0 || y.Top == 0)
            //{
            //    return -1;
            //}

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

            if (x is Tablix && y is Line && x.Top < y.Top)
            {
                y.Style.Position = "";
            }

            if (y is Tablix && x is Line && y.Top < x.Top)
            {
                x.Style.Position = "";
            }

            if (x.Top > y.Top)
            {
                if (x is Line && x.Style.Position == "absolute")
                {
                    // x top is higher, line is absolute so don't consider height.
                    return 1;
                }
                
                if (x.Top > y.Top + y.Height && (!(y is Line) || (y is Line && y.Style.Position == "")))
                {   
                    // New row.
                    return 1;
                }
                else if (x.Top > y.Top && y is Line && y.Style.Position == "absolute")
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
                if (x is Line && x.Style.Position == "absolute")
                {
                    // x top is lower, line is absolute so don't consider height.
                    return -1;
                }

                if (y.Top > x.Top + x.Height && (!(x is Line) || (x is Line && x.Style.Position == "")))
                {                    
                    return -1;
                }
                else if (y.Top > x.Top && x is Line && x.Style.Position == "absolute")
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
