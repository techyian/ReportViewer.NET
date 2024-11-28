using ReportViewer.NET.Comparers;
using ReportViewer.NET.Parsers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{    
    public class Tablix : ReportItem
    {
        public List<TablixCornerRow> TablixCornerRows { get; private set; }
        public TablixBody TablixBodyObj { get; private set; }
        public TablixHierarchy TablixColumnHierarchy { get; private set; }
        public TablixHierarchy TablixRowHierarchy { get; private set; }        
        public List<TablixSort> TablixSortExpressions { get; set; }
        public PageBreak PageBreak { get; private set; }
        public int TotalTopLevelGroupCount { get; set; }
        public int RequestedTablixPage { get; set; } = 1;

        public Tablix(XElement tablix, IEnumerable<DataSet> datasets, ReportRDL report, ReportItem parent)
            : base(tablix, report, parent)  
        {
            // Inspect report metadata and look for requested page.
            if (report.Metadata.Any(m => m.Key == ReportMetadata.TablixPageKey && m.ObjectName == this.Name))
            {
                var md = report.Metadata.First(m => m.Key == ReportMetadata.TablixPageKey && m.ObjectName == this.Name);
                this.RequestedTablixPage = int.Parse(md.Value);
            }

            this.DataSets = datasets;
            this.TablixCornerRows = new List<TablixCornerRow>();
            this.TablixBodyObj = new TablixBody(this, tablix.Element(report.Namespace + "TablixBody"));
            this.TablixSortExpressions = new List<TablixSort>();

            this.DataSetName = tablix.Element(report.Namespace + "DataSetName")?.Value;

            var cornerRows = tablix.Element(report.Namespace + "TablixCorner")?.Elements(report.Namespace + "TablixCornerRows")?.Elements(report.Namespace + "TablixCornerRow");
            var trh = tablix.Elements(report.Namespace + "TablixRowHierarchy").LastOrDefault();
            var tch = tablix.Elements(report.Namespace + "TablixColumnHierarchy").LastOrDefault();
            var tablixSorts = tablix.Elements(this.Report.Namespace + "SortExpressions")?.Elements(report.Namespace + "SortExpression");

            this.TablixRowHierarchy = new TablixHierarchy(trh, this);
            this.TablixColumnHierarchy = new TablixHierarchy(tch, this);

            if (cornerRows != null)
            {
                foreach (var cr in cornerRows)
                {
                    this.TablixCornerRows.Add(new TablixCornerRow(this, cr, this));
                }
            }

            if (!string.IsNullOrEmpty(this.DataSetName))
            {
                this.DataSetReference = new DataSetReference()
                {
                    DataSetName = this.DataSetName
                };

                this.DataSetReference.DataSet = datasets.FirstOrDefault(ds => ds.Name == this.DataSetReference.DataSetName);                
            }

            var pageBreak = tablix.Element(report.Namespace + "PageBreak")?.Element(report.Namespace + "BreakLocation")?.Value;

            if (pageBreak != null)
            {
                switch (pageBreak.ToLower())
                {
                    case "start":
                        this.PageBreak = PageBreak.Start;
                        break;
                    case "end":
                        this.PageBreak = PageBreak.End;
                        break;
                    case "startandend":
                        this.PageBreak = PageBreak.StartAndEnd;
                        break;
                    // Between can't be used for report items.
                }
            }

            if (tablixSorts != null)
            {
                foreach (var tablixSort in tablixSorts)
                {                    
                    this.TablixSortExpressions.Add(new TablixSort(this, tablixSort));
                }                                            
            }
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            switch (this.PageBreak)
            {
                case PageBreak.Start:
                case PageBreak.StartAndEnd:
                    sb.AppendLine("<div class=\"reportitem-break-start\"></div>");
                    break;                
            }

            if (!this.Hidden || (this.Hidden && this.Report.ToggleItemRequests.Contains(this.ToggleItem)))
            {
                this.Hidden = false;
                this.Style.Hidden = false;

                var bodyObj = this.TablixBodyObj?.Build();

                if (this.TotalTopLevelGroupCount > 1 && this.PageBreak != PageBreak.None)
                {
                    sb.AppendLine("<div style=\"display: block; width: 100%;\">");
                    sb.AppendLine("<div class=\"reportviewer-table-pager\">");
                    sb.AppendLine($"<button type=\"button\" data-tablename=\"{this.Name}\" data-direction=\"prev\">Prev</button>");
                    sb.AppendLine($"<button type=\"button\" data-tablename=\"{this.Name}\" data-direction=\"next\">Next</button>");
                    sb.AppendLine("</div>");                    
                }
                                
                sb.AppendLine($"<table {this.Style?.Build()} class=\"reportviewer-table\" data-toggle=\"{this.ToggleItem}\">");
                sb.AppendLine(bodyObj);
                sb.AppendLine("</table>");
                
                if (this.TotalTopLevelGroupCount > 1)
                {
                    sb.AppendLine("</div>");
                }
            }
                        
            switch (this.PageBreak)
            {                
                case PageBreak.StartAndEnd:
                    sb.AppendLine("<div class=\"reportitem-break-start\"></div>");
                    break;
                case PageBreak.End:
                    sb.AppendLine("<div class=\"reportitem-break-end\"></div>");
                    break;
            }

            this.Values = null;

            return sb.ToString();
        }
    }

    public class TablixBody
    {
        public List<TablixColumn> TablixColumns { get; private set; }
        public List<TablixRow> TablixRows { get; private set; }        
        public Tablix Tablix { get; private set; }        
        public int TotalTablixRowHeaders { get; set; }
                
        internal TablixBody(Tablix tablix, XElement tablixBody)
        {
            this.Tablix = tablix;
            this.TablixColumns = new List<TablixColumn>();
            this.TablixRows = new List<TablixRow>();
                        
            var columns = tablixBody.Elements(this.Tablix.Report.Namespace + "TablixColumns").Elements(this.Tablix.Report.Namespace + "TablixColumn");
            var rows = tablixBody.Elements(this.Tablix.Report.Namespace + "TablixRows").Elements(this.Tablix.Report.Namespace + "TablixRow");
            
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
                    this.TablixRows.Add(new TablixRow(this, r, this.Tablix));
                }
            }            
        }

        public string Build()
        {
            var tablixBody = string.Empty;

            if (this.Tablix.TablixRowHierarchy.TablixMembers.Any())
            {
                tablixBody = this.BuildWithRowHierarchy();
            }
            else
            {
                tablixBody = this.BuildNoRowHierarchy();
            }

            return tablixBody;
        }

        /// <summary>
        /// Recursive method which will find sub sort tablix members and apply the <see cref="TablixMemberSortComparer"/> class.
        /// </summary>
        /// <param name="tablixMember">The current tablix member, we expect this to have a sort expression.</param>
        /// <param name="baseComparer">The base comparer which may have parents.</param>
        /// <param name="dataSet">The current dataset which will have been ordered at least once previously.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> dataset.</returns>
        public IOrderedEnumerable<IDictionary<string, object>> SortTablixMember(TablixMember tablixMember, TablixMemberSortComparer baseComparer, IOrderedEnumerable<IDictionary<string, object>> dataSet)
        {
            // TODO: Handle descending order.
            var subMembersWithSortExpression = tablixMember.TablixMembers.Where(tm => tm.TablixMemberSort != null);

            if (!subMembersWithSortExpression.Any())
            {
                return dataSet;
            }

            foreach (var subMember in subMembersWithSortExpression)
            {
                if (this.Tablix.DataSetReference != null && this.Tablix.DataSetReference.DataSet.DataSetResults != null)
                {
                    // Order dataset results by expression.                    
                    var fieldsIdx = subMember.TablixMemberSort.SortExpression.IndexOf("Fields!");
                    var fieldEnd = subMember.TablixMemberSort.SortExpression.IndexOf('.', fieldsIdx);
                    var fieldName = subMember.TablixMemberSort.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                    var comparer = new TablixMemberSortComparer(fieldName, baseComparer);

                    return this.SortTablixMember(subMember, comparer, dataSet.Order(comparer));
                }
            }

            return dataSet;
        }

        private string BuildNoRowHierarchy()
        {
            var sb = new StringBuilder();

            this.Sort(null, this.Tablix.DataSetReference?.DataSet?.DataSetResults);

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

                if (row.ContainsRepeatExpression && this.Tablix.DataSetReference != null && this.Tablix.DataSetReference.DataSet.DataSetResults != null)
                {                                        
                    foreach (var result in this.Tablix.DataSetReference.DataSet.DataSetResults)
                    {
                        row.Values = result;

                        sb.AppendLine($"<tr height=\"{row.Height}\">");
                        sb.AppendLine(row.Build(this.Tablix));
                        sb.AppendLine("</tr>");
                    }
                    
                }
                else
                {
                    sb.AppendLine($"<tr height=\"{row.Height}\">");
                    sb.AppendLine(row.Build(this.Tablix));
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");

            return sb.ToString();
        }

        private string BuildWithRowHierarchy()
        {
            var sb = new StringBuilder();
            var rowsSb = new StringBuilder();
            var colsSb = new StringBuilder();

            sb.AppendLine("<tbody>");

            var rowIdx = 0;
            var colIdx = 0;
            var tablixHierarchyStructure = new TablixHierarchyGroupStructure()
            {
                CurrentGroupPage = 1
            };

            // At the top-level, each of the <TablixMember> elements will be their own row. We then look in each member to determine grouping, searching and nested row relationships.
            for (var i = 0; i < this.Tablix.TablixRowHierarchy.TablixMembers.Count; i++)
            {
                rowIdx = this.ProcessRowHierarchyTablixMembers(
                    rowsSb, 
                    null, 
                    null, 
                    this.Tablix.TablixRowHierarchy.TablixMembers[i], 
                    new List<TablixMember>(), 
                    rowIdx, 
                    PageBreak.None, 
                    tablixHierarchyStructure, 
                    false);

                // Clear keys picked up from previous top level rows.
                this.Tablix.GroupedResultsKeys.Clear();

                // Need to reset grouped results, otherwise any aggregated results in the next member which needs to use non-grouped data will be incorrect.
                if (this.Tablix.GroupedResults != null && !this.Tablix.Parents.Any(ri => ri.GetType() == typeof(Tablix)))
                {
                    this.Tablix.GroupedResults = null;
                    this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = null;                    
                }
            }

            if (this.Tablix.TablixCornerRows.Count > 0)
            {
                for (var i = 0; i < this.Tablix.TablixCornerRows.Count; i++)
                {
                    colsSb.AppendLine($"<tr data-colspan-start>");
                    colsSb.AppendLine(this.Tablix.TablixCornerRows[i].Build(this.Tablix));

                    if (this.Tablix.TablixColumnHierarchy.TablixMembers.Count > 0)
                    {
                        // When a tablix column spans multiple rows, these are declared as nested headers within the TablixColumnHierarchy TablixMembers.
                        // The code below is an attempt to determine which row we're on via the current TablixCornerRow index and search the TablixColumnHierarchy 
                        // tree to find the appropriate member to render, also making sure we pass through any previous tablix members to sort out grouping and sorting.
                        for (var j = 0; j < this.Tablix.TablixColumnHierarchy.TablixMembers.Count; j++)
                        {
                            var membersUptoHierarchy = new List<TablixMember>();
                            this.FindAllMembersUptoAtHierarchyLevel(this.Tablix.TablixColumnHierarchy.TablixMembers[j], membersUptoHierarchy, i + 1);

                            this.ProcessColumnHierarchyTablixMembers(
                                colsSb,
                                null,
                                null,
                                membersUptoHierarchy.Last(),
                                membersUptoHierarchy.Take(membersUptoHierarchy.Count - 1).ToList(),
                                j // is this right?
                            );

                            
                        }
                    }

                    colsSb.AppendLine($"</tr>");
                }
            }
            // May end up needing to rework columns, hiding empty members for now.
            else if (this.Tablix.TablixColumnHierarchy != null && this.Tablix.TablixColumnHierarchy.TablixMembers.Any(tm => !tm.IsEmpty))
            {
                // Similar to the above, but because we don't have the guaranteed rows from TablixCornerRows, we need to do a blind search 
                // on how far down our column hierarchy goes, rendering a row for each header found.
                var complete = false;
                var i = 0;

                while (!complete)
                {
                    colsSb.AppendLine($"<tr data-colspan-start>");

                    for (var j = 0; j < this.Tablix.TablixColumnHierarchy.TablixMembers.Count; j++)
                    {
                        var membersUptoHierarchy = new List<TablixMember>();
                        this.FindAllMembersUptoAtHierarchyLevel(this.Tablix.TablixColumnHierarchy.TablixMembers[j], membersUptoHierarchy, i + 1);

                        if (!membersUptoHierarchy.Any())
                        {
                            complete = true;
                            break;
                        }

                        colIdx = this.ProcessColumnHierarchyTablixMembers(
                            colsSb,
                            null,
                            null,
                            membersUptoHierarchy.Last(),
                            membersUptoHierarchy.Take(membersUptoHierarchy.Count - 1).ToList(),
                            j // is this right?
                        );
                    }

                    colsSb.AppendLine($"</tr>");

                    i++;
                }                                
            }

            this.Tablix.GroupedResultsKeys.Clear();
            this.Tablix.GroupedResults = null;
            this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = null;

            sb.AppendLine(colsSb.ToString());
            sb.AppendLine(rowsSb.ToString());
            sb.AppendLine("</tbody>");

            return sb.ToString();
        }

        private int AllSubMembersCount(TablixMember member, int currentCount)
        {
            if (member.TablixMembers.Any())
            {
                foreach (var submember in member.TablixMembers)
                {
                    currentCount++;
                    currentCount = this.AllSubMembersCount(submember, currentCount);
                }
            }

            return currentCount;
        }

        private void FindAllSubMembers(TablixMember currentMember, List<TablixMember> allMembers)
        {            
            if (currentMember.TablixMembers.Any())
            {                
                foreach (var tm in currentMember.TablixMembers)
                {
                    this.FindAllSubMembers(tm, allMembers);
                }
            }
            else
            {
                allMembers.Add(currentMember);
            }
        }

        private void FindAllMembersUptoAtHierarchyLevel(TablixMember start, List<TablixMember> foundMembers, int hierarchyLevel)
        {
            if (start.HierarchyLevel <= hierarchyLevel)
            {
                foundMembers.Add(start);
            }

            foreach (var tm in start.TablixMembers)
            {
                this.FindAllMembersUptoAtHierarchyLevel(tm, foundMembers, hierarchyLevel);
            }
        }

        private void FindAllSubGroupHeaders(TablixMember currentMember, List<TablixMember> allHeaders)
        {
            if (currentMember.TablixHeader != null)
            {
                allHeaders.Add(currentMember);
            }

            if (currentMember.TablixMembers.Any(tm => tm.TablixHeader != null))
            {
                foreach (var tm in currentMember.TablixMembers)
                {
                    this.FindAllSubGroupHeaders(tm, allHeaders);
                }
            }
        }

        private int ProcessColumnHierarchyTablixMembers(
            StringBuilder sb,
            List<IDictionary<string, object>> dataSetResults,
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,
            int currentColumnIndx
        )
        {
            var dsr = this.Sort(tablixMember, this.Tablix.DataSetReference.DataSet.DataSetResults);
            
            var groupedDs = this.Group(tablixMember, prevTablixMembers);

            if (groupedDs != null)
            {
                this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = groupedDs;
            }

            // Scenario: column spans multiple rows and is part of column group, always need to process cells for every grouped result found
            // check whether current member is a group, in which case GroupedDataSetResults will be recalculated, or otherwise check whether member has any parents
            // which are groups and if so, we re-use current calculated GroupedDataSetResults.
            if (this.Tablix?.DataSetReference?.DataSet?.GroupedDataSetResults != null && (tablixMember.TablixMemberGroup != null || prevTablixMembers.Any(tm => tm.TablixMemberGroup != null)))
            {
                // Starting a new grouping
                var after = 0;
                var groupedResults = this.Tablix.DataSetReference.DataSet.GroupedDataSetResults;

                foreach (var groupedResult in groupedResults)
                {                   
                    this.Tablix.GroupedResults = groupedResult;

                    if (tablixMember.ToggleItem != null)
                    {
                        tablixMember.ToggleItemKey = string.Join('-', this.Tablix.GroupedResultsKeys);
                    }

                    if (!this.Tablix.GroupedResultsKeys.Contains(groupedResult.Key.ToString().Replace(" ", "")))
                    {
                        this.Tablix.GroupedResultsKeys.Add(groupedResult.Key.ToString().Replace(" ", ""));
                    }

                    var before = currentColumnIndx;

                    var prevMembersBeforeGroup = new List<TablixMember>(prevTablixMembers);
                    var headers = new List<TablixMember>();
                    
                    prevTablixMembers.Add(tablixMember);

                    this.FindAllSubGroupHeaders(tablixMember, headers);

                    currentColumnIndx = this.ProcessColumnHierarchyResults(currentColumnIndx, tablixMember, prevTablixMembers, groupResults, sb);
                 
                    this.Tablix.GroupedResultsKeys.Remove(groupedResult.Key.ToString().Replace(" ", ""));

                    // As previous headers will have information stored against them from previous groupings we need to clear that information out. 
                    // We also need to make sure that headers at the same hierarchy level have their grouping information cleared.
                    var lastHeader = tablixMember.TablixHeader != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixHeader != null);

                    if (lastHeader != null)
                    {
                        foreach (var memberAtSameLevel in headers.Where(tm => tm.HierarchyLevel == lastHeader.HierarchyLevel || tm.HierarchyLevel > lastHeader.HierarchyLevel))
                        {
                            memberAtSameLevel.TablixHeader.InsertedKey = false;
                            memberAtSameLevel.TablixHeader.KeyGuid = "";
                        }
                    }

                    if (tablixMember.TablixHeader != null)
                    {
                        tablixMember.TablixHeader.GroupCount = tablixMember.TablixHeader.AdditionalMemberCount = 0;
                    }

                    prevTablixMembers = new List<TablixMember>(prevMembersBeforeGroup);                    
                    after = currentColumnIndx;
                    currentColumnIndx = before;
                }

                currentColumnIndx = after;
            }           
            else
            {
                if (tablixMember.ToggleItem != null)
                {
                    tablixMember.ToggleItemKey = string.Join('-', this.Tablix.GroupedResultsKeys);
                }
                                
                currentColumnIndx = this.ProcessColumnHierarchyResults(currentColumnIndx, tablixMember, prevTablixMembers, groupResults, sb);
            }

            return currentColumnIndx;
        }

        private int ProcessRowHierarchyTablixMembers(
            StringBuilder sb, 
            List<IDictionary<string, object>> dataSetResults, 
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,            
            int currentRowIndx,
            PageBreak pageBreak,
            TablixHierarchyGroupStructure tablixHierarchyStructure,
            bool isNested)
        {
            var dsr = this.Tablix.DataSetReference.DataSet.DataSetResults;

            if (this.Tablix.GroupedResults == null)
            {
                // Let's not sort the initial DataSetResults again as we're now grouping which will be sorted in the 
                // Group method below.
                this.Tablix.DataSetReference.DataSet.DataSetResults = this.Sort(tablixMember, this.Tablix.DataSetReference.DataSet.DataSetResults).ToList();
            }
            
            var groupedDs = this.Group(tablixMember, prevTablixMembers);

            if (groupedDs != null)
            {
                this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = groupedDs;
            }
                                    
            // If current tablix member is a group, then any <TablixMember> elements which are at the same level are classed as being related
            // to the current member. If the member is not a group in its own right, then any <TablixMember> elements at the same level are for other rows.
          
            if (tablixMember.TablixMemberGroup != null && pageBreak == PageBreak.None)
            {
                pageBreak = tablixMember.TablixMemberGroup.PageBreak;
            }

            if (this.Tablix?.DataSetReference?.DataSet?.GroupedDataSetResults != null && tablixMember.TablixMemberGroup != null && tablixMember.TablixMemberGroup.GroupExpression != null)
            {
                // Starting a new grouping
                var after = 0;
                var groupIdx = 0;
                var groupedResults = this.Tablix.DataSetReference.DataSet.GroupedDataSetResults;

                tablixHierarchyStructure.TotalPages = groupedResults.Count;

                foreach (var groupedResult in groupedResults)
                {
                    var lastTablix = this.Tablix.Parents.LastOrDefault(ri => ri.GetType() == typeof(Tablix));
                    
                    if (lastTablix?.GroupedResults == null && !prevTablixMembers.Any(tm => tm.TablixMemberGroup != null) && this.Tablix.TotalTopLevelGroupCount == 0)
                    {
                        this.Tablix.TotalTopLevelGroupCount = groupedResults.Count;
                    }

                    // This statement will ensure that we only print the top level group which matches the requested page from the front end,
                    // or the user has requested all pages to be shown. If we're nested within a parent tablix which has its own grouping, then check for this too.
                    if (this.Tablix.RequestedTablixPage == -1 || 
                        prevTablixMembers.Any(tm => tm.TablixMemberGroup != null) || 
                        groupIdx + 1 == this.Tablix.RequestedTablixPage ||
                        lastTablix?.GroupedResults != null || 
                        this.Tablix.PageBreak == PageBreak.None)
                    {
                        this.Tablix.GroupedResults = groupedResult;

                        if (tablixMember.ToggleItem != null)
                        {
                            tablixMember.ToggleItemKey = string.Join('-', this.Tablix.GroupedResultsKeys);
                        }
                     
                        if (!this.Tablix.GroupedResultsKeys.Contains(groupedResult.Key.ToString().Replace(" ", "")))
                        {
                            this.Tablix.GroupedResultsKeys.Add(groupedResult.Key.ToString().Replace(" ", ""));
                        }

                        if (tablixMember.TablixHeader != null)
                        {
                            var currentRow = this.TablixRows[currentRowIndx];
                            var prevHeaders = prevTablixMembers.Where(t => t.TablixHeader != null);

                            if (prevHeaders.Any())
                            {
                                foreach (var ph in prevHeaders)
                                {
                                    // Here we are wanting to apply the group count of the current grouping to the previous header so we can accurately 
                                    // state rowcount against the row.
                                    ph.TablixHeader.GroupCount = groupedResults.Count > ph.TablixHeader.GroupCount ? groupedResults.Count : ph.TablixHeader.GroupCount;
                                    ph.TablixHeader.AdditionalMemberCount = ph.TablixMembers.Count();

                                    if (ph.TablixMembers.Count() > 0)
                                    {
                                        ph.TablixHeader.AdditionalMemberCount--;
                                    }
                                }
                            }

                            // From the current hierarchy level we're at, find the child members searching to lowest level. We then subtract 1 which represents the grouped result set.
                            var subMembers = new List<TablixMember>();
                            this.FindAllSubMembers(tablixMember, subMembers);

                            tablixMember.TablixHeader.AdditionalMemberCount = subMembers.Count - 1;

                            if (!tablixMember.TablixMembers.Any(tm => tm.TablixMemberGroup != null) && currentRow.ContainsAggregatorExpression && !currentRow.ContainsRepeatExpression)
                            {
                                // We're the last member and the row is aggregated without any repeats. Just rowspan 1.
                                tablixMember.TablixHeader.GroupCount = 1;
                            }
                        }

                        var before = currentRowIndx;

                        var prevMembersBeforeGroup = new List<TablixMember>(prevTablixMembers);
                        var headers = new List<TablixMember>();

                        prevTablixMembers.Add(tablixMember);

                        this.FindAllSubGroupHeaders(tablixMember, headers);

                        if (tablixMember.TablixMembers.Any())
                        {
                            var cmIdx = 0;
                            foreach (var childMember in tablixMember.TablixMembers)
                            {
                                currentRowIndx = this.ProcessRowHierarchyTablixMembers(
                                    sb, 
                                    dataSetResults, 
                                    groupedResult, 
                                    childMember, 
                                    prevTablixMembers, 
                                    currentRowIndx, 
                                    pageBreak, 
                                    tablixHierarchyStructure,
                                    cmIdx + 1 < tablixMember.TablixMembers.Count ? true : false
                                );

                                prevTablixMembers.Remove(childMember);
                                cmIdx++;
                            }
                        }
                        else
                        {
                            currentRowIndx = this.ProcessRowHierarchyResults(currentRowIndx, tablixMember, prevTablixMembers, groupedResult, tablixHierarchyStructure, pageBreak, sb, isNested);
                        }

                        this.Tablix.GroupedResults = null;
                        this.Tablix.GroupedResultsKeys.Remove(groupedResult.Key.ToString().Replace(" ", ""));

                        // As previous headers will have information stored against them from previous groupings we need to clear that information out. 
                        // We also need to make sure that headers at the same hierarchy level have their grouping information cleared.
                        var lastHeader = tablixMember.TablixHeader != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixHeader != null);

                        if (lastHeader != null)
                        {
                            foreach (var memberAtSameLevel in headers.Where(tm => tm.HierarchyLevel == lastHeader.HierarchyLevel || tm.HierarchyLevel > lastHeader.HierarchyLevel))
                            {
                                memberAtSameLevel.TablixHeader.InsertedKey = false;
                                memberAtSameLevel.TablixHeader.KeyGuid = "";
                            }
                        }

                        if (tablixMember.TablixHeader != null)
                        {
                            tablixMember.TablixHeader.GroupCount = tablixMember.TablixHeader.AdditionalMemberCount = 0;
                        }

                        prevTablixMembers = new List<TablixMember>(prevMembersBeforeGroup);
                        tablixHierarchyStructure.NewGroup();
                        tablixHierarchyStructure.CurrentGroupPage = tablixHierarchyStructure.CurrentGroupPage + 1;
                        after = currentRowIndx;
                        currentRowIndx = before;
                    }
                                                            
                    groupIdx++;
                }

                currentRowIndx = after;
            }
            else if (tablixMember.TablixMembers.Any())
            {
                if (tablixMember.ToggleItem != null)
                {
                    tablixMember.ToggleItemKey = string.Join('-', this.Tablix.GroupedResultsKeys);
                }

                prevTablixMembers.Add(tablixMember);

                var cmIdx = 0;
                foreach (var childMember in tablixMember.TablixMembers)
                {                    
                    currentRowIndx = this.ProcessRowHierarchyTablixMembers(
                        sb, 
                        dataSetResults, 
                        groupResults, 
                        childMember, 
                        prevTablixMembers, 
                        currentRowIndx, 
                        pageBreak, 
                        tablixHierarchyStructure,
                        cmIdx + 1 < tablixMember.TablixMembers.Count ? true : false
                    );
                }
            }
            else
            {                
                if (tablixMember.ToggleItem != null)
                {
                    tablixMember.ToggleItemKey = string.Join('-', this.Tablix.GroupedResultsKeys);
                }

                currentRowIndx = this.ProcessRowHierarchyResults(currentRowIndx, tablixMember, prevTablixMembers, groupResults, tablixHierarchyStructure, pageBreak, sb, isNested);                
            }
                        
            return currentRowIndx;
        }

        private int ProcessColumnHierarchyResults(
            int currentColumnIndx,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,
            IGrouping<object, IDictionary<string, object>> groupResults,
            StringBuilder sb
            )
        {
            var column = this.TablixColumns[currentColumnIndx];
            var newKey = this.Tablix.GroupedResultsKeys.Any() ? string.Join('-', this.Tablix.GroupedResultsKeys) : $"col-{currentColumnIndx}";

            if (groupResults != null && groupResults.Any())
            {
                column.GroupedResult = groupResults.ElementAt(0);
            }

            var lastGroup = tablixMember.TablixMemberGroup != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixMemberGroup != null);
            var lastHeader = tablixMember.TablixHeader != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixHeader != null);
            var headersFoundInTablixMembers = prevTablixMembers.Where(t => t.TablixHeader != null).ToList();

            if (tablixMember.TablixHeader != null && !object.ReferenceEquals(tablixMember, prevTablixMembers.LastOrDefault(t => t.TablixHeader != null)))
            {
                // Make sure we add the last header found if applicable.
                headersFoundInTablixMembers.Add(tablixMember);
            }

            if (lastHeader != null && lastHeader.TablixHeader != null)
            {
                lastHeader.TablixHeader.GroupedResults = groupResults;
            }
                
            if (lastHeader != null && lastHeader.TablixHeader != null)
            {
                sb.AppendLine(lastHeader.TablixHeader.Build());
                lastHeader.TablixHeader.InsertedKey = true;
            }

            return currentColumnIndx + 1;            
        }

        private int ProcessRowHierarchyResults(
            int currentRowIndx,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixHierarchyGroupStructure tablixHierarchyStructure,
            PageBreak pageBreak,
            StringBuilder sb,
            bool isNested
        )
        {
            if (this.TablixRows.Count <= currentRowIndx)
            {
                // The TablixRowHierarchy structure does not seem to be robust and tally up with the number of rows expected, either that or my understanding 
                // is wrong. For now putting this in to make sure it doesn't cry.
                currentRowIndx--;
            }

            var row = this.TablixRows[currentRowIndx];
            var newKey = this.Tablix.GroupedResultsKeys.Any() ? string.Join('-', this.Tablix.GroupedResultsKeys) : $"row-{currentRowIndx}";

            row.GroupedResults = groupResults;

            var lastGroup = tablixMember.TablixMemberGroup != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixMemberGroup != null);
            var lastHeader = tablixMember.TablixHeader != null ? tablixMember : prevTablixMembers.LastOrDefault(t => t.TablixHeader != null);
            var headersFoundInTablixMembers = prevTablixMembers.Where(t => t.TablixHeader != null).ToList();

            if (tablixMember.TablixHeader != null && !object.ReferenceEquals(tablixMember, prevTablixMembers.LastOrDefault(t => t.TablixHeader != null)))
            {
                // Make sure we add the last header found if applicable.
                headersFoundInTablixMembers.Add(tablixMember);
            }

            if (headersFoundInTablixMembers.Count > this.Tablix.TablixBodyObj.TotalTablixRowHeaders)
            {
                this.Tablix.TablixBodyObj.TotalTablixRowHeaders = headersFoundInTablixMembers.Count;
            }

            if (lastHeader != null && lastHeader.TablixHeader != null)
            {
                lastHeader.TablixHeader.GroupedResults = groupResults;
            }

            if (lastHeader != null &&
                lastHeader.TablixHeader != null &&
                (lastHeader.TablixHeader.ContainsRepeatExpression || row.ContainsRepeatExpression) &&
                !lastHeader.TablixHeader.ContainsAggregatorExpression &&               
                this.Tablix.DataSetReference != null &&
                this.Tablix.DataSetReference.DataSet.DataSetResults != null)
            {
                this.ProcessTablixRowHierarchyMember(
                    groupResults, 
                    tablixMember, 
                    lastGroup, 
                    lastHeader, 
                    row, 
                    tablixHierarchyStructure, 
                    headersFoundInTablixMembers, 
                    prevTablixMembers,
                    pageBreak, 
                    newKey, 
                    sb,
                    isNested
                );
            }
            else if (lastGroup != null &&                    
                     this.Tablix.DataSetReference != null &&
                     this.Tablix.DataSetReference.DataSet.DataSetResults != null

                )
            {
                this.ProcessTablixRowHierarchyMember(
                    groupResults, 
                    tablixMember, 
                    lastGroup, 
                    lastHeader, 
                    row, 
                    tablixHierarchyStructure, 
                    headersFoundInTablixMembers, 
                    prevTablixMembers,
                    pageBreak, 
                    newKey, 
                    sb,
                    isNested
                );
            }
            else
            {                
                sb.AppendLine(
                    lastGroup != null ?
                    $"<tr height=\"{row.Height}\" data-grouped-result=\"true\">" :
                    $"<tr height=\"{row.Height}\" data-grouped-result=\"false\">"
                );

                if (headersFoundInTablixMembers.Any())
                {
                    foreach (var header in headersFoundInTablixMembers)
                    {
                        if ((header.TablixHeader.ContainsRepeatExpression && !header.TablixHeader.InsertedKey) || !header.TablixHeader.ContainsRepeatExpression)
                        {
                            sb.AppendLine(header.TablixHeader.Build(0, row.ContainsAggregatorExpression, row.ContainsRepeatExpression, this.Tablix));
                        }
                    }
                }
                else if (lastHeader != null && lastHeader.TablixHeader != null && ((lastHeader.TablixHeader.ContainsRepeatExpression && !lastHeader.TablixHeader.InsertedKey) || !lastHeader.TablixHeader.ContainsRepeatExpression))
                {
                    sb.AppendLine(lastHeader.TablixHeader.Build(0, row.ContainsAggregatorExpression, row.ContainsRepeatExpression, this.Tablix));
                }

                if ((!tablixMember.Hidden || (tablixMember.Hidden && this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tablixMember.ToggleItemKey))) && 
                    !prevTablixMembers.Any(tm => tm.Hidden && !this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tm.ToggleItemKey)))
                {
                    if (this.Tablix.DataSetReference?.DataSet != null)
                    {
                        this.Tablix.DataSetReference.DataSet.CurrentRowNumber = currentRowIndx + 1;
                    }
                    
                    row.CurrentRowNumber = currentRowIndx + 1;
                    sb.AppendLine(row.Build(this.Tablix));
                }

                sb.AppendLine("</tr>");                            
            }

            return currentRowIndx + 1;
        }

        private void ProcessTablixRowHierarchyMember(
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixMember tablixMember,
            TablixMember lastGroup,
            TablixMember lastHeader,
            TablixRow row,
            TablixHierarchyGroupStructure tablixHierarchyStructure,
            IEnumerable<TablixMember> headersFoundInTablixMembers,
            List<TablixMember> prevTablixMembers,
            PageBreak pageBreak,
            string newKey,
            StringBuilder sb,
            bool isNested
            )
        {
            var currentRowNum = 1;

            List<IDictionary<string, object>> results = null;

            if (groupResults != null)
            {
                results = groupResults.ToList();
            }
            else
            {
                results = this.Tablix.DataSetReference.DataSet.DataSetResults;
            }
            
            tablixHierarchyStructure.KeyGuid = newKey;
                                               
            foreach (var result in results)
            {
                row.Values = result;
                row.CurrentRowNumber = currentRowNum;
                row.KeyGuid = tablixHierarchyStructure.KeyGuid;

                if (this.Tablix.DataSetReference?.DataSet != null)
                {
                    this.Tablix.DataSetReference.DataSet.CurrentRowNumber = currentRowNum;
                }

                var dataPageBreak = "";
                var dataGroupResultsCount = $"data-tablepages=\"{tablixHierarchyStructure.TotalPages}\"";
                var dataGroupedResult = groupResults != null ? "data-grouped-result=\"true\"" : "data-grouped-result=\"false\"";
                var dataPageNumber = $"data-rowtablepage=\"{tablixHierarchyStructure.CurrentGroupPage}\"";

                switch (pageBreak)
                {
                    case PageBreak.Start:
                        dataPageBreak = "data-pagebreak-start";
                        break;
                    case PageBreak.StartAndEnd:
                        dataPageBreak = "data-pagebreak-start data-pagebreak-end";
                        break;
                    case PageBreak.End:
                        dataPageBreak = "data-pagebreak-end";
                        break;
                    case PageBreak.Between:
                        dataPageBreak = "data-pagebreak-between";
                        break;
                }

                // With grouping, we only want to show the resolved expression value on the first row, all subsequent rows within this group will show an empty 
                // cell in the first column.
                   
                var createRow = false;
                var doShow = ((!tablixMember.Hidden || (tablixMember.Hidden && this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tablixMember.ToggleItemKey))) &&
                                !prevTablixMembers.Any(tm => tm.Hidden && !this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tm.ToggleItemKey)));
                var rowCount = results.Count();

                if (doShow)
                {
                    createRow = true;
                }
                    
                // If this is a row that should be hidden, but we have headers that haven't been created yet then we should create those.
                // We also want to ensure that we're not creating empty rows unnecessarily, so the below will break out if we've created the headers and will also
                // ensure that the "rowspan" attribute against the header key is set to 1 when children are hidden.
                if (!doShow && (headersFoundInTablixMembers.Any() || lastHeader != null ))
                {
                    if (currentRowNum == 1)
                    {
                        rowCount = 1;
                    }
                    else
                    {
                        break;
                    }                                                
                }
                
                if (createRow || 
                    ((headersFoundInTablixMembers.Any(h => !h.TablixHeader.InsertedKey) || (lastHeader != null && !lastHeader.TablixHeader.InsertedKey)) && !isNested))
                {
                    sb.AppendLine($"<tr height=\"{row.Height}\" {dataGroupedResult} data-rowspan-start {dataPageBreak} {dataGroupResultsCount} {dataPageNumber} data-row-key=\"{newKey}\">");
                }
                                        
                if (headersFoundInTablixMembers.Any())
                {
                    foreach (var header in headersFoundInTablixMembers)
                    {
                        header.Values = result;
                        if (!header.TablixHeader.InsertedKey)
                        {
                            header.TablixHeader.KeyGuid = newKey;
                                                                
                            sb.AppendLine(header.TablixHeader.Build(rowCount, row.ContainsAggregatorExpression, row.ContainsRepeatExpression, this.Tablix));

                            header.TablixHeader.InsertedKey = true;                                    
                        }
                    }
                }
                else if (lastHeader != null)
                {                            
                    lastHeader.TablixHeader.KeyGuid = newKey;
                    lastHeader.Values = result;                            
                    sb.AppendLine(lastHeader.TablixHeader.Build(rowCount, row.ContainsAggregatorExpression, row.ContainsRepeatExpression, this.Tablix));

                    lastHeader.TablixHeader.InsertedKey = true;
                }

                if (doShow)
                {                         
                    sb.AppendLine(row.Build(this.Tablix));
                }
                    
                if (createRow ||
                    ((headersFoundInTablixMembers.Any(h => !h.TablixHeader.InsertedKey) || (lastHeader != null && !lastHeader.TablixHeader.InsertedKey)) && !isNested))
                {
                    sb.AppendLine("</tr>");
                }
                                        
                // I don't know if this is right or just plain hacky. If the current TablixMember is a group and has children then we know that this instance
                // just needs to be rendered once. However, if the TablixMember is a group and has a header that should be repeated then we should loop over the results.
                // Honestly, at this stage I'm just making this stuff up.
                if (tablixMember.ContainsReportItemWithSubGroup || 
                    (tablixMember.IsGroupHasMember && (tablixMember.TablixHeader == null || (tablixMember.TablixHeader != null && !tablixMember.TablixHeader.ContainsRepeatExpression))) || 
                    row.ContainsAggregatorExpression)
                {
                    break;
                }

                currentRowNum++;
            }           
        }
        private IEnumerable<IDictionary<string, object>> Sort(TablixMember tablixMember, IEnumerable<IDictionary<string, object>> dsr)
        {
            if (this.Tablix.TablixSortExpressions.Any())
            {
                TablixMemberSortComparer previousComparer = null;

                foreach (var ts in this.Tablix.TablixSortExpressions)
                {
                    // Order dataset results by expression.                    
                    var fieldsIdx = ts.SortExpression.IndexOf("Fields!");
                    var fieldEnd = ts.SortExpression.IndexOf('.', fieldsIdx);
                    var fieldName = ts.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                    var baseComparer = new TablixMemberSortComparer(fieldName, previousComparer);
                                        
                    if (dsr != null)
                    {
                        ts.Sorted = true;

                        dsr = dsr.Order(baseComparer);
                        previousComparer = baseComparer;
                    }
                }
            }

            if (tablixMember != null && tablixMember.TablixMemberSort != null && dsr != null)
            {                
                // Order dataset results by expression.                    
                var fieldsIdx = tablixMember.TablixMemberSort.SortExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberSort.SortExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberSort.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                var baseComparer = new TablixMemberSortComparer(fieldName, null);

                if (dsr != null)
                {
                    tablixMember.TablixMemberSort.Sorted = true;

                    return this.SortTablixMember(tablixMember, baseComparer, dsr.Order(baseComparer)).ToList();
                }
            }

            return dsr;
        }

        private IGrouping<object, IDictionary<string, object>> SortGroup(TablixMember tablixMember, IGrouping<object, IDictionary<string, object>> dsr)
        {
            if (tablixMember.TablixMemberSort != null && dsr != null)
            {
                // Order dataset results by expression.                    
                var fieldsIdx = tablixMember.TablixMemberSort.SortExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberSort.SortExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberSort.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                var baseComparer = new TablixMemberSortComparer(fieldName, null);

                if (dsr != null)
                {                    
                    return this.SortTablixMember(tablixMember, baseComparer, dsr.Order(baseComparer)).ToList().GroupBy(g => dsr.Key).FirstOrDefault();
                }
            }

            return dsr;
        }

        private List<IGrouping<object, IDictionary<string, object>>> Group(TablixMember tablixMember, List<TablixMember> prevTablixMembers)
        {
            if (tablixMember.TablixMemberGroup != null &&
                !string.IsNullOrEmpty(tablixMember.TablixMemberGroup.GroupExpression) &&
                this.Tablix.DataSetReference != null &&
                this.Tablix.DataSetReference.DataSet.DataSetResults != null
            )
            {
                // Group items to dictionary.
                var fieldsIdx = tablixMember.TablixMemberGroup.GroupExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberGroup.GroupExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberGroup.GroupExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();

                if (this.Tablix.GroupedResults != null)
                {
                    if (!this.Tablix.GroupedResultsKeys.Contains(this.Tablix.GroupedResults.Key.ToString().Replace(" ", "")))
                    {
                        this.Tablix.GroupedResultsKeys.Add(this.Tablix.GroupedResults.Key.ToString().Replace(" ", ""));
                    }

                    return this.SortGroup(tablixMember, this.Tablix.GroupedResults).GroupBy(g => g[fieldName]).ToList();
                }
                else
                {
                    return this.Sort(tablixMember, this.Tablix.DataSetReference.DataSet.DataSetResults).GroupBy(g => g[fieldName]).ToList();
                }
            }
            else if (this.Tablix.Parents.Any(ri => ri.GetType() == typeof(Tablix)) && !prevTablixMembers.Any(tm => tm.TablixMemberGroup != null && tm.TablixMemberGroup.GroupExpression != null))
            {
                // Reset grouped results to be that of parent tablix (if applicable) if we're not within a group.
                var lastTablix = this.Tablix.Parents.Last(ri => ri.GetType() == typeof(Tablix));

                if (lastTablix.GroupedResults != null)
                {
                    this.Tablix.GroupedResults = lastTablix.GroupedResults;

                    // Have we been provided with group results from further up the tree?                
                    return new List<IGrouping<object, IDictionary<string, object>>>() { this.SortGroup(tablixMember, this.Tablix.GroupedResults) };
                }
            }           

            return null;
        }
    }

    public class TablixColumn
    {
        public string Width { get; set; }
        public TablixBody Body { get; set; }
        public IDictionary<string, object> GroupedResult { get; set; }

        internal TablixColumn(TablixBody body, XElement column)
        {
            this.Body = body;
            this.Width = column.Element(this.Body.Tablix.Report.Namespace + "Width")?.Value;
        }

        public string Build()
        {
            return @"<td width=""" + Width + @"""></td>";
        }
    }

    public class TablixRow : ReportItem
    {        
        public List<TablixCell> TablixCells { get; set; }
        public TablixBody Body { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public bool ContainsAggregatorExpression { get; set; }        
        public string KeyGuid { get; set; }
        
        internal TablixRow(TablixBody body, XElement row, ReportItem parent)
            : base(row, body.Tablix.Report, parent)
        {
            this.Body = body;     
            this.TablixCells = new List<TablixCell>();

            var cells = row.Elements(this.Body.Tablix.Report.Namespace + "TablixCells").Elements(this.Body.Tablix.Report.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    this.TablixCells.Add(new TablixCell(c, this.Body.Tablix.Report, this));

                    if (!this.ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        this.ContainsRepeatExpression = ExpressionParser.ContainsRepeatExpression(c.Value);
                    }

                    if (!this.ContainsAggregatorExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        // TODO: Add other aggregator functions.
                        this.ContainsAggregatorExpression = ExpressionParser.ContainsAggregatorExpression(c.Value);
                    }
                }
            }
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            if (this.TablixCells != null)
            {
                for (var i = 0; i < this.TablixCells.Count;)
                {
                    var emptyCells = 0;
                    for (var j = i + 1; j < this.TablixCells.Count; j++)
                    {
                        if (this.TablixCells[j].TablixCellContent.Count == 0)
                        {
                            emptyCells++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (this.Body.Tablix.TablixColumnHierarchy.TablixMembers.Any(tm => !tm.IsEmpty))
                    {
                        var columnMember = this.Body.Tablix.TablixColumnHierarchy.TablixMembers[i];

                        if (columnMember.TablixMemberGroup != null && !string.IsNullOrEmpty(columnMember.TablixMemberGroup.GroupExpression))
                        {
                            var sorted = this.Sort(columnMember, this.GroupedResults);
                            var groupedByColumnMember = this.Group(columnMember, sorted);
                            var groupedResultsBefore = this.GroupedResults?.ToList().GroupBy(g => this.GroupedResults.Key).FirstOrDefault();

                            foreach (var groupedResults in groupedByColumnMember)
                            {
                                this.Body.Tablix.GroupedResults = groupedResults;

                                if (this.TablixCells[i].TablixCellContent.Count > 0)
                                {
                                    sb.AppendLine(emptyCells > 0 ? $"<td colspan=\"{emptyCells + 1}\">" : "<td>");
                                }

                                this.GroupedResults = groupedResults;

                                sb.AppendLine(this.TablixCells[i].Build(this));

                                if (this.TablixCells[i].TablixCellContent.Count > 0)
                                {
                                    sb.AppendLine("</td>");
                                }
                            }

                            this.GroupedResults = groupedResultsBefore;
                            this.Body.Tablix.GroupedResults = groupedResultsBefore;
                        }
                        else
                        {
                            if (this.TablixCells[i].TablixCellContent.Count > 0)
                            {
                                sb.AppendLine(emptyCells > 0 ? $"<td colspan=\"{emptyCells + 1}\">" : "<td>");
                            }

                            sb.AppendLine(this.TablixCells[i].Build(this));

                            if (this.TablixCells[i].TablixCellContent.Count > 0)
                            {
                                sb.AppendLine("</td>");
                            }
                        }
                    }
                    else
                    {
                        if (this.TablixCells[i].TablixCellContent.Count > 0)
                        {
                            sb.AppendLine(emptyCells > 0 ? $"<td colspan=\"{emptyCells + 1}\">" : "<td>");
                        }

                        sb.AppendLine(this.TablixCells[i].Build(this));

                        if (this.TablixCells[i].TablixCellContent.Count > 0)
                        {
                            sb.AppendLine("</td>");
                        }
                    }

                    if (emptyCells > 0)
                    {
                        i += emptyCells;
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            this.Values = null;

            return sb.ToString();
        }

        private IEnumerable<IDictionary<string, object>> Sort(TablixMember tablixMember, IEnumerable<IDictionary<string, object>> dsr)
        {
            if (dsr == null)
            {
                dsr = this.Body.Tablix.DataSetReference.DataSet.DataSetResults;
            }

            if (tablixMember.TablixMemberSort != null && dsr != null)
            {
                // Order dataset results by expression.                    
                var fieldsIdx = tablixMember.TablixMemberSort.SortExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberSort.SortExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberSort.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                var baseComparer = new TablixMemberSortComparer(fieldName, null);

                if (dsr != null)
                {
                    tablixMember.TablixMemberSort.Sorted = true;

                    return this.Body.SortTablixMember(tablixMember, baseComparer, dsr.Order(baseComparer)).ToList();
                }
            }

            return dsr;
        }

        private List<IGrouping<object, IDictionary<string, object>>> Group(TablixMember tablixMember, IEnumerable<IDictionary<string, object>> dsr)
        {
            if (dsr == null)
            {
                dsr = this.Body.Tablix.DataSetReference.DataSet.DataSetResults;
            }

            if (!string.IsNullOrEmpty(tablixMember.TablixMemberGroup.GroupExpression) && dsr != null)
            {
                // Group items to dictionary.
                var fieldsIdx = tablixMember.TablixMemberGroup.GroupExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberGroup.GroupExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberGroup.GroupExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                                
                return dsr.GroupBy(g => g[fieldName]).ToList();                
            }

            return null;
        }
    }

    public class TablixCornerRow : ReportItem
    {
        public List<TablixCell> TablixCornerCells { get; private set; }
        public Tablix Tablix { get; private set; }


        internal TablixCornerRow(Tablix tablix, XElement cornerRow, ReportItem parent)
            : base (cornerRow, tablix.Report, parent)
        {
            this.Tablix = tablix;
            this.TablixCornerCells = new List<TablixCell>();

            var tablixCornerCells = cornerRow.Elements(this.Tablix.Report.Namespace + "TablixCornerCell");

            if (tablixCornerCells != null)
            {
                foreach (var tcc in tablixCornerCells)
                {
                    this.TablixCornerCells.Add(new TablixCell(tcc, this.Tablix.Report, this));
                }
            }
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            if (this.TablixCornerCells != null)
            {
                for (var i = 0; i < this.TablixCornerCells.Count;)
                {
                    var emptyCells = 0;
                    for (var j = i + 1; j < this.TablixCornerCells.Count; j++)
                    {
                        if (this.TablixCornerCells[j].TablixCellContent.Count == 0)
                        {
                            emptyCells++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (this.TablixCornerCells[i].TablixCellContent.Count > 0)
                    {
                        sb.AppendLine(emptyCells > 0 ? $"<td colspan=\"{emptyCells + 1}\">" : "<td>");
                    }

                    sb.AppendLine(this.TablixCornerCells[i].Build(this));

                    if (this.TablixCornerCells[i].TablixCellContent.Count > 0)
                    {
                        sb.AppendLine("</td>");
                    }
                    
                    if (emptyCells > 0)
                    {
                        i += emptyCells;
                    }
                    else
                    {
                        i++;
                    }
                }
            }

            this.Values = null;

            return sb.ToString();
        }
    }

    public class TablixCell : ReportItem
    {
        public List<ReportItem> TablixCellContent { get; private set; }
        public TablixRow Row { get; private set; }
        public TablixHeader Header { get; private set; }        
        public TablixCornerRow CornerRow { get; private set; }

        internal TablixCell(XElement cell, ReportRDL report, ReportItem parent)
            : base(cell, report, parent)
        {
            if (parent is TablixRow)
            {
                this.Row = (TablixRow)parent;
            }

            if (parent is TablixHeader)
            {
                this.Header = (TablixHeader)parent;
            }

            if (parent is TablixCornerRow)
            {
                this.CornerRow = (TablixCornerRow)parent;
            }

            this.TablixCellContent = new List<ReportItem>();

            var cellContents = cell.Elements(this.Report.Namespace + "CellContents");
            var reportItems = ReportItem.ParseElements(cellContents, report, report.DataSets, this, parent);

            this.TablixCellContent.AddRange(reportItems);
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            for (var j = 0; j < this.TablixCellContent.Count; j++)
            {
                var content = this.TablixCellContent[j];

                if (content is SubReport)
                {
                    var sr = (SubReport)content;
                    var srRdl = sr.GetSubReportRDL();
                    var layoutProvider = new LayoutProvider();
                    var finalUserParamsForSubReport = new List<ReportParameter>();

                    // Retrieve all parameters for the sub report.
                    foreach (var p in srRdl.ReportParameters)
                    {
                        // Search user provided parameters first
                        if (this.Report.UserProvidedParameters.Any(rp => rp.Name == p.Name))
                        {
                            finalUserParamsForSubReport.Add(this.Report.UserProvidedParameters.First(rp => rp.Name == p.Name));
                        }
                        // Try to retrieve the parameter from the tablix row.
                        else
                        {
                            var dataSetResults = this.GroupedResults?.Select(r => r).ToList() ?? this.DataSetReference?.DataSet?.DataSetResults;
                            var parsedValue = this.Report.Parser.ParseReportExpressionString(
                                p.Value, 
                                dataSetResults, 
                                (IDictionary<string, object>)this.Values,
                                this.CurrentRowNumber,
                                this.DataSets, 
                                this.DataSetReference?.DataSet, 
                                null
                            );

                            if (parsedValue != null)
                            {
                                finalUserParamsForSubReport.Add(new ReportParameter
                                {
                                    Name = p.Name,
                                    DataType = p.DataType,
                                    Value = Convert.ToString(parsedValue)
                                });
                            }
                        }
                    }

                    if (finalUserParamsForSubReport.Count != srRdl.ReportParameters.Count)
                    {
                        continue;
                    }

                    sb.AppendLine(layoutProvider.PublishReportOutput(srRdl, finalUserParamsForSubReport, this.Report.ToggleItemRequests, this.Report.Metadata).GetAwaiter().GetResult().Value);
                }
                else
                {
                    sb.AppendLine(content.Build(this));
                }
            }

            this.Values = null;

            return sb.ToString();
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

            var tablixMemberElements = element.Elements(this.Tablix.Report.Namespace + "TablixMembers").Elements(this.Tablix.Report.Namespace + "TablixMember");

            foreach (var member in tablixMemberElements)
            {                
                this.TablixMembers.Add(new TablixMember(member, this, tablixMemberElements.Count(), true, 1, this.Tablix));                                
            }
        }
    }

    public class TablixMember : ReportItem
    {
        public string Id { get; set; }
        public TablixHierarchy TablixHierarchy { get; set; }
        public TablixMemberGroup TablixMemberGroup { get; set; }
        public TablixMemberSort TablixMemberSort { get; set; }
        public TablixHeader TablixHeader { get; set; }
        public List<TablixMember> TablixMembers { get; set; }        
        public string KeepWithGroup { get; set; }        
        public string ToggleItemKey { get; set; }
        public bool RepeatOnNewPage { get; set; }
        public bool KeepTogether { get; set; }
        public bool IsGroupHasMember => this.TablixMemberGroup != null && this.TablixMembers.Any();        
        public bool ContainsReportItemWithSubGroup { get; set; }
        public bool IsRootMember { get; private set; }
        public int HierarchyLevel { get; private set; }
        public bool IsEmpty { get; set; }

        public TablixMember(XElement element, TablixHierarchy tablixHierarchy, int outerTablixMembersCount, bool isRootMember, int hierarchyLevel, ReportItem parent)
            : base (element, tablixHierarchy.Tablix.Report, parent)
        {
            this.Id = Guid.NewGuid().ToString().Split('-')[0];
            this.TablixHierarchy = tablixHierarchy;
            this.TablixMembers = new List<TablixMember>();
            this.IsRootMember = isRootMember;
            this.HierarchyLevel = hierarchyLevel;
            this.IsEmpty = element.IsEmpty;
                        
            var tablixGroup = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Group");
            var tablixSort = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "SortExpressions")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "SortExpression");
            var tablixHeader = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "TablixHeader");
            var tablixMemberElements = element.Elements(this.TablixHierarchy.Tablix.Report.Namespace + "TablixMembers")?.Elements(this.TablixHierarchy.Tablix.Report.Namespace + "TablixMember");
            
            var isHidden = this.TablixHierarchy.Tablix.Report.Parser.ParseReportExpressionString(
                element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Visibility")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Hidden")?.Value,
                null,
                null,
                0,
                this.TablixHierarchy.Tablix.Report.DataSets,
                null,
                null
            );

            var toggleItem = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Visibility")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "ToggleItem")?.Value;

            this.KeepWithGroup = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "KeepWithGroup")?.Value;
            this.KeepTogether = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "KeepTogether")?.Value == "true";
            this.RepeatOnNewPage = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "RepeatOnNewPage")?.Value == "true";
            this.ContainsReportItemWithSubGroup = this.TablixHierarchy.Tablix.XElement.Element(this.TablixHierarchy.Tablix.Report.Namespace + "TablixBody").Descendants(this.TablixHierarchy.Tablix.Report.Namespace + "Group").Any();

            if (isHidden is bool)
            {
                this.Hidden = (bool)isHidden;
            }
            else
            {
                // Assume string?
                this.Hidden = isHidden?.ToString() == "true";
            }

            if (toggleItem != null)
            {
                this.ToggleItem = toggleItem;
            }

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
                this.TablixHeader = new TablixHeader(this, tablixHeader, this.TablixHierarchy.Tablix);
            }
                
            if (tablixMemberElements != null)
            {
                var membersToProcess = new List<TablixMember>();
                
                foreach (var member in tablixMemberElements)
                {                    
                    membersToProcess.Add(new TablixMember(member, this.TablixHierarchy, tablixMemberElements.Count(), false, hierarchyLevel + 1, this));                    
                }

                // Treat all <TablixMember> elements individually.
                foreach (var member in membersToProcess)
                {
                    this.TablixMembers.Add(member);
                }                               
            }    
                        
            if (this.Hidden)
            {
                this.TablixHierarchy.Tablix.Report.HiddenTablixMembers.Add(this);
            }
        }

        public override string Build(ReportItem parent)
        {
            throw new NotImplementedException();
        }
    }

    public class TablixHeader : ReportItem
    {
        public TablixMember TablixMember { get; set; }
        public string Size { get; set; }
        public TablixCell TablixHeaderContent { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public bool ContainsAggregatorExpression { get; set; }        
        public bool InsertedKey { get; set; }        
        public string KeyGuid { get; set; }
        public int GroupCount { get; set; }
        public int AdditionalMemberCount { get; set; }
        
        public TablixHeader(TablixMember tablixMember, XElement element, ReportItem parent)
            : base (element, tablixMember.TablixHierarchy.Tablix.Report, parent)
        {
            this.TablixMember = tablixMember;
            this.Size = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "Size")?.Value;
            this.TablixHeaderContent = new TablixCell(element, this.TablixMember.TablixHierarchy.Tablix.Report, this);
                        
            this.ContainsAggregatorExpression = ExpressionParser.ContainsAggregatorExpression(element?.Value ?? string.Empty);
            this.ContainsRepeatExpression = ExpressionParser.ContainsRepeatExpression(element?.Value ?? string.Empty);
        }

        public override string Build(ReportItem parent)
        {
            throw new NotImplementedException();
        }

        public string Build(int rowCount, bool rowContainsAggregatedExpression, bool rowContainsRepeatExpression, ReportItem parent)
        {
            this.NestedCopy(parent, this);

            var sb = new StringBuilder();

            if (this.TablixHeaderContent.TablixCellContent.Count > 0)
            {
                if (rowContainsAggregatedExpression && !rowContainsRepeatExpression)
                {
                    rowCount = this.GroupCount + this.AdditionalMemberCount;
                }
                else
                {
                    rowCount += this.AdditionalMemberCount;
                }

                var widthValue = Style.ConvertUnit(this.Size);
                var cellStyle = !string.IsNullOrEmpty(widthValue) ? $"style=\"width:{widthValue}\"" : "";
                var rowSpan = rowCount > 0 ? $"rowspan=\"{rowCount}\"" : "";

                sb.AppendLine(!this.InsertedKey ? $"<td {cellStyle} data-group-key {rowSpan}>" : "<td>");
                foreach (var cell in this.TablixHeaderContent.TablixCellContent)
                {
                    sb.AppendLine(cell.Build(this.TablixMember.TablixHierarchy.Tablix));
                }
                sb.AppendLine("</td>");
            }

            this.Values = null;

            return sb.ToString();
        }

        public string Build()
        {
            var sb = new StringBuilder();

            if (this.TablixHeaderContent.TablixCellContent.Count > 0)
            {                
                sb.AppendLine(!this.InsertedKey ? $"<td data-group-key>" : "<td>");
                foreach (var cell in this.TablixHeaderContent.TablixCellContent)
                {
                    sb.AppendLine(cell.Build(this.TablixMember.TablixHierarchy.Tablix));
                }
                sb.AppendLine("</td>");
            }

            return sb.ToString();
        }
    }

    public class TablixMemberGroup
    {
        public TablixMember TablixMember { get; set; }
        public string Name { get; set; }
        public string GroupExpression { get; set; }
        public PageBreak PageBreak { get; set; }

        public TablixMemberGroup(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;
            this.Name = element.Attribute("Name")?.Value;
            this.GroupExpression = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "GroupExpressions")?.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "GroupExpression")?.Value;
            
            var pageBreak = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "PageBreak")?.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "BreakLocation")?.Value;

            if (pageBreak != null)
            {
                switch (pageBreak.ToLower())
                {
                    case "start":
                        this.PageBreak = PageBreak.Start;
                        break;
                    case "end":
                        this.PageBreak = PageBreak.End;
                        break;
                    case "startandend":
                        this.PageBreak = PageBreak.StartAndEnd;
                        break;
                    case "between":
                        this.PageBreak = PageBreak.Between;
                        break;
                }
            }
        }
    }

    public class TablixSort
    {
        public string SortExpression { get; set; }
        public Tablix Tablix { get; set; }
        public bool Sorted { get; set; }

        public TablixSort(Tablix tablix, XElement element)
        {
            this.Tablix = tablix;
            this.SortExpression = element.Element(this.Tablix.Report.Namespace + "Value")?.Value;
        }
    }

    public class TablixMemberSort
    {        
        public string SortExpression { get; set; }
        public TablixMember TablixMember { get; set; }
        public bool Sorted { get; set; }

        public TablixMemberSort(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;            
            this.SortExpression = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "Value")?.Value;
        }
    }
}
