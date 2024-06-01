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

        private TablixBody TablixBodyObj { get; set; }

        public Tablix(XElement tablix, IEnumerable<DataSet> datasets)
            : base(tablix)
        {
            DataSets = datasets;
            TablixBodyObj = new TablixBody(this, tablix.Element(Namespace + "TablixBody"));

            DataSetName = tablix.Element(Namespace + "DataSetName")?.Value;
            Hidden = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "Hidden")?.Value == "true";
            ToggleItem = tablix.Element(Namespace + "Visibility")?.Element(Namespace + "ToggleItem")?.Value;
                        
            if (!string.IsNullOrEmpty(DataSetName))
            {
                DataSetReference = new DataSetReference()
                {
                    DataSetName = DataSetName
                };

                DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == DataSetReference.DataSetName);
            }
        }

        public override string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<table {Style?.Build()} class=\"reportviewer-table\">");
            sb.AppendLine(TablixBodyObj?.Build());
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
            Tablix = tablix;
            TablixColumns = new List<TablixColumn>();
            TablixRows = new List<TablixRow>();

            var columns = tablixBody.Elements(ReportItem.Namespace + "TablixColumns").Elements(ReportItem.Namespace + "TablixColumn");
            var rows = tablixBody.Elements(ReportItem.Namespace + "TablixRows").Elements(ReportItem.Namespace + "TablixRow");

            if (columns != null)
            {
                foreach (var c in columns)
                {
                    TablixColumns.Add(new TablixColumn(this, c));
                }
            }

            if (rows != null)
            {
                foreach (var r in rows)
                {
                    TablixRows.Add(new TablixRow(this, r));
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            foreach (var column in TablixColumns)
            {
                sb.AppendLine(column.Build());
            }
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");

            sb.AppendLine("<tbody>");

            for (var i = 0; i < TablixRows.Count; i++)
            {
                var row = TablixRows[i];

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
            Body = body;
            Width = column.Element(ReportItem.Namespace + "Width")?.Value;
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
            Body = body;
            Height = row.Element(ReportItem.Namespace + "Height")?.Value;
            TablixCells = new List<TablixCell>();

            var cells = row.Elements(ReportItem.Namespace + "TablixCells").Elements(ReportItem.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    TablixCells.Add(new TablixCell(this, c));

                    if (!ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        ContainsRepeatExpression = !LayoutProvider.CountRegex.IsMatch(c.Value) && LayoutProvider.FieldRegex.IsMatch(c.Value);
                    }
                }
            }
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<tr height=""" + Height + @""">");

            if (TablixCells != null)
            {
                foreach (var cell in TablixCells)
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

        internal TablixCell(TablixRow row, XElement cell)
        {
            Row = row;
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
                            TablixCellContent.Add(new Textbox(this, textbox, Row.Body.Tablix.DataSets));
                        }
                    }

                    // Process other types.
                }
            }
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
