using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{    
    public class Tablix : ReportItem
    {   
        public string DataSetName { get; set; }
        public DataSetReference DataSetReference { get; set; }
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }        
        public IEnumerable<DataSet> DataSets { get; set; }
        public TablixBody TablixBodyObj { get; set; }
        public TablixHierarchy TablixColumnHierarchy { get; set; }
        public TablixHierarchy TablixRowHierarchy { get; set; }

        public Tablix(XElement tablix, IEnumerable<DataSet> datasets)
            : base(tablix)
        {
            this.DataSets = datasets;
            this.TablixBodyObj = new TablixBody(this, tablix.Element(Namespace + "TablixBody"));

            this.DataSetName = tablix.Element(Namespace + "DataSetName")?.Value;
            this.Hidden = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "Hidden")?.Value == "true";
            this.ToggleItem = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "ToggleItem")?.Value;
            this.TablixRowHierarchy = new TablixHierarchy(tablix.Element(Namespace + "TablixRowHierarchy"), this);
            this.TablixColumnHierarchy = new TablixHierarchy(tablix.Element(Namespace + "TablixColumnHierarchy"), this);

            if (!string.IsNullOrEmpty(this.DataSetName))
            {
                this.DataSetReference = new DataSetReference()
                {
                    DataSetName = this.DataSetName
                };

                this.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == this.DataSetReference.DataSetName);
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<table {this.Style?.Build()} class=\"reportviewer-table\">");
            sb.AppendLine(this.TablixBodyObj?.Build());
            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }

    public class TablixBody
    {
        public List<TablixColumn> TablixColumns { get; set; }
        public List<TablixRow> TablixRows { get; set; }
        public Tablix Tablix { get; set; }

        internal TablixBody(Tablix tablix, XElement tablixBody)
        {
            this.Tablix = tablix;
            this.TablixColumns = new List<TablixColumn>();
            this.TablixRows = new List<TablixRow>();

            var columns = tablixBody.Elements(ReportItem.Namespace + "TablixColumns").Elements(ReportItem.Namespace + "TablixColumn");
            var rows = tablixBody.Elements(ReportItem.Namespace + "TablixRows").Elements(ReportItem.Namespace + "TablixRow");

            if (columns != null)
            {
                foreach (var c in columns)
                {
                    this.TablixColumns.Add(new TablixColumn(this, c));
                }
            }

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    this.TablixRows.Add(new TablixRow(this, r));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            foreach (var column in this.TablixColumns)
            {
                sb.AppendLine(column.Build());
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");

            for (var i = 0; i < this.TablixRows.Count; i++)
            {
                var row = this.TablixRows[i];

                if (row.ContainsRepeatExpression && row.Body.Tablix.DataSetReference != null && row.Body.Tablix.DataSetReference.DataSet.DataSetResults != null)
                {
                    foreach (var result in row.Body.Tablix.DataSetReference.DataSet.DataSetResults)
                    {
                        row.Values = result;

                        sb.AppendLine("<tr>");
                        sb.AppendLine(row.Build());
                        sb.AppendLine("</tr>");
                    }
                }
                else
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine(row.Build());
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");

            return sb.ToString();
        }
    }

    public class TablixColumn
    {
        public string Width { get; set; }
        public TablixBody Body { get; set; }

        internal TablixColumn(TablixBody body, XElement column)
        {
            this.Body = body;
            this.Width = column.Element(ReportItem.Namespace + "Width")?.Value;
        }

        public string Build()
        {
            return @"<td width=""" + Width + @"""></td>";
        }
    }

    public class TablixRow
    {
        public string Height { get; set; }
        public List<TablixCell> TablixCells { get; set; }
        public TablixBody Body { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public dynamic Values { get; set; }

        internal TablixRow(TablixBody body, XElement row)
        {
            this.Body = body;
            this.Height = row.Element(ReportItem.Namespace + "Height")?.Value;
            this.TablixCells = new List<TablixCell>();

            var cells = row.Elements(ReportItem.Namespace + "TablixCells").Elements(ReportItem.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    this.TablixCells.Add(new TablixCell(this, c));

                    if (!ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        this.ContainsRepeatExpression = !LayoutProvider.CountRegex.IsMatch(c.Value) && LayoutProvider.FieldRegex.IsMatch(c.Value);
                    }
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<tr height=""" + Height + @""">");

            if (this.TablixCells != null)
            {
                foreach (var cell in this.TablixCells)
                {
                    sb.AppendLine("<td>");
                    if (cell.TablixCellContent != null)
                    {
                        for (var i = 0; i < cell.TablixCellContent.Count; i++)
                        {
                            var content = cell.TablixCellContent[i];

                            sb.AppendLine(content.Build());
                        }
                    }
                    sb.AppendLine("</td>");
                }
            }

            sb.AppendLine("</tr>");

            return sb.ToString();
        }
    }

    public class TablixCell
    {
        public List<ReportItem> TablixCellContent { get; set; }
        public TablixRow Row { get; set; }
        public TablixHeader Header { get; set; }

        internal TablixCell(TablixRow row, XElement cell)
            : this(cell)
        {
            this.Row = row;            
        }

        internal TablixCell(TablixHeader tablixHeader, XElement cell)
            : this(cell)
        {
            this.Header = tablixHeader;            
        }

        internal TablixCell(XElement cell)
        {
            TablixCellContent = new List<ReportItem>();

            var cellContents = cell.Elements(ReportItem.Namespace + "CellContents");
            
            if (cellContents != null)
            {                
                foreach (var c in cellContents)
                {
                    var textboxes = c.Elements(ReportItem.Namespace + "Textbox");

                    if (textboxes != null)
                    {
                        foreach (var textbox in textboxes)
                        {
                            IEnumerable<DataSet> dataSets = null;

                            if (this.Row != null)
                            {
                                dataSets = this.Row.Body.Tablix.DataSets;
                            }
                            else if (this.Header != null)
                            {
                                dataSets = this.Header.TablixMember.TablixHierarchy.Tablix.DataSets;
                            }

                            TablixCellContent.Add(new Textbox(this, textbox, dataSets));
                        }
                    }

                    // Process other types.
                }
            }
        }
    }

    public class TablixHierarchy
    {
        public Tablix Tablix { get; set; }
        public List<TablixMember> TablixMembers { get; set; }

        public TablixHierarchy(XElement element, Tablix tablix)
        {
            this.Tablix = tablix;
            this.TablixMembers = new List<TablixMember>();

            var tablixMemberElements = element.Elements(ReportItem.Namespace + "TablixMembers").Elements(ReportItem.Namespace + "TablixMember");

            foreach (var member in tablixMemberElements)
            {
                this.TablixMembers.Add(new TablixMember(member, this));
            }
        }
    }

    public class TablixMember
    {
        public TablixHierarchy TablixHierarchy { get; set; }
        public TablixMemberGroup TablixMemberGroup { get; set; }
        public TablixMemberSort TablixMemberSort { get; set; }
        public TablixHeader TablixHeader { get; set; }

        public TablixMember(XElement element, TablixHierarchy tablixHierarchy)
        {
            this.TablixHierarchy = tablixHierarchy;

            var tablixGroup = element.Element(ReportItem.Namespace + "Group");
            var tablixSort = element.Element(ReportItem.Namespace + "SortExpressions")?.Element(ReportItem.Namespace + "SortExpression");
            var tablixHeader = element.Element(ReportItem.Namespace + "TablixHeader");

            if (tablixGroup != null)
            {
                this.TablixMemberGroup = new TablixMemberGroup(this, tablixGroup);
            }
                        
            if (tablixSort != null)
            {
                this.TablixMemberSort = new TablixMemberSort(this, tablixSort);
            }
                        
            if (tablixHeader != null)
            {
                this.TablixHeader = new TablixHeader(this, tablixHeader);
            }                        
        }
    }

    public class TablixHeader
    {
        public TablixMember TablixMember { get; set; }
        public string Size { get; set; }
        public TablixCell TablixHeaderContent { get; set; }
        
        public TablixHeader(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;
            this.Size = element.Element(ReportItem.Namespace + "Size")?.Value;
            this.TablixHeaderContent = new TablixCell(this, element);
        }
    }

    public class TablixMemberGroup
    {
        public TablixMember TablixMember { get; set; }
        public string Name { get; set; }
        public string GroupExpression { get; set; }
        
        public TablixMemberGroup(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;
            this.Name = element.Attribute("Name")?.Value;
            this.GroupExpression = element.Element(ReportItem.Namespace + "GroupExpressions").Element(ReportItem.Namespace + "GroupExpression")?.Value;
        }
    }

    public class TablixMemberSort
    {        
        public string SortExpression { get; set; }
        public TablixMember TablixMember { get; set; }

        public TablixMemberSort(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;            
            this.SortExpression = element.Element(ReportItem.Namespace + "Value")?.Value;
        }
    }

    public class TablixExpression
    {
        public int Index { get; set; } = -1;
        public int EndIndex { get; set; }
        public TablixOperator Operator { get; set; }
        public string Field { get; set; }
        public Type ResolvedType { get; set; }
        public object Value { get; set; }
        public string DataSetName { get; set; }
    }

    public enum TablixOperator
    {
        None,
        Count,
        Field,
        Add,
        Subtract,
        Multiply,
        Divide
    }
}
