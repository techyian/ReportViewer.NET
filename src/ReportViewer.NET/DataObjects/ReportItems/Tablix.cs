using HtmlAgilityPack;
using ReportViewer.NET.Comparers;
using ReportViewer.NET.Parsers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{    
    public class Tablix : ReportItem
    {           
        public TablixBody TablixBodyObj { get; set; }
        public TablixHierarchy TablixColumnHierarchy { get; set; }
        public TablixHierarchy TablixRowHierarchy { get; set; }        
        public PageBreak PageBreak { get; set; }

        public Tablix(XElement tablix, IEnumerable<DataSet> datasets, ReportRDL report, ReportItem parent)
            : base(tablix, report, parent)  
        {
            this.DataSets = datasets;            
            this.TablixBodyObj = new TablixBody(this, tablix.Element(report.Namespace + "TablixBody"));
            
            this.DataSetName = tablix.Element(report.Namespace + "DataSetName")?.Value;

            var trh = tablix.Elements(report.Namespace + "TablixRowHierarchy").LastOrDefault();
            var tch = tablix.Elements(report.Namespace + "TablixColumnHierarchy").LastOrDefault();

            this.TablixRowHierarchy = new TablixHierarchy(trh, this);
            this.TablixColumnHierarchy = new TablixHierarchy(tch, this);

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
                sb.AppendLine($"<table {this.Style?.Build()} class=\"reportviewer-table\" data-toggle=\"{this.ToggleItem}\">");
                sb.AppendLine(this.TablixBodyObj?.Build());
                sb.AppendLine("</table>");
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

            return sb.ToString();
        }
    }

    public class TablixBody
    {
        public List<TablixColumn> TablixColumns { get; set; }
        public List<TablixRow> TablixRows { get; set; }
        public Tablix Tablix { get; set; }        

        internal ExpressionParser Parser { get; set; }

        internal TablixBody(Tablix tablix, XElement tablixBody)
        {
            this.Tablix = tablix;
            this.TablixColumns = new List<TablixColumn>();
            this.TablixRows = new List<TablixRow>();
            this.Parser = new ExpressionParser();

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
                    this.TablixRows.Add(new TablixRow(this, r));
                }
            }
        }

        public string Build()
        {
            var tablixBody = string.Empty;

            //if (this.Tablix.TablixRowHierarchy.TablixMembers.Any(t => t.TablixHeader != null))
            if (this.Tablix.TablixRowHierarchy.TablixMembers.Any())
            {
                tablixBody = this.BuildWithRowHierarchy();
            }
            else
            {
                tablixBody = this.BuildNoRowHierarchy();
            }

            // Use HTMLAgilityPack to fix rowspans.
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(tablixBody);

            var tablixRows = htmlDoc.DocumentNode.SelectNodes("//tr[@data-rowspan-start = '']");

            if (tablixRows != null)
            {
                foreach (var tr in tablixRows)
                {
                    int groupCount = 1;
                    HtmlNode sibling = tr.NextSibling;

                    while (sibling != null)
                    {
                        // Break out if we're starting a new rowspan, i.e. new group key, or if we've entered a new row which is ungrouped.
                        if (sibling.Name == "tr" && (sibling.Attributes.Any(a => a.Name == "data-rowspan-start") || sibling.Attributes.Any(a => a.Name == "data-grouped-result" && a.Value == "false")))
                        {
                            break;
                        }

                        if (sibling.Name == "tr" && !sibling.Attributes.Any(a => a.Name == "data-rowspan-start"))
                        {
                            groupCount++;
                        }

                        sibling = sibling.NextSibling;
                    }

                    var groupKeys = tr.SelectNodes("./td[@data-group-key = '']");

                    if (groupKeys != null)
                    {
                        foreach (var gk in groupKeys)
                        {
                            gk.Attributes.Add("rowspan", groupCount.ToString());
                        }                        
                    }
                }

                using (var sw = new StringWriter())
                {
                    htmlDoc.Save(sw);

                    return sw.ToString();
                }
            }

            return tablixBody;
        }

        private string BuildNoRowHierarchy()
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

                if (row.ContainsRepeatExpression && this.Tablix.DataSetReference != null && this.Tablix.DataSetReference.DataSet.DataSetResults != null)
                {                                        
                    foreach (var result in this.Tablix.DataSetReference.DataSet.DataSetResults)
                    {
                        row.Values = result;

                        sb.AppendLine($"<tr height=\"{row.Height}\">");
                        sb.AppendLine(row.Build());
                        sb.AppendLine("</tr>");
                    }
                    
                }
                else
                {
                    sb.AppendLine($"<tr height=\"{row.Height}\">");
                    sb.AppendLine(row.Build());
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");

            return sb.ToString();
        }

        private string BuildWithRowHierarchy()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<tbody>");

            var rowIdx = 0;
            var tablixHierarchyStructure = new TablixHierarchyGroupStructure()
            {
                CurrentGroupPage = 1
            };
            
            // At the top-level, each of the <TablixMember> elements will be their own row. We then look in each member to determine grouping, searching and nested row relationships.
            for (var i = 0; i < this.Tablix.TablixRowHierarchy.TablixMembers.Count; i++)
            {
                rowIdx = this.ProcessTablixMembers(sb, null, null, this.Tablix.TablixRowHierarchy.TablixMembers[i], new List<TablixMember>(), rowIdx, PageBreak.None, tablixHierarchyStructure);

                // Clear keys picked up from previous top level rows.
                this.Tablix.GroupedResultsKeys.Clear();

                // Need to reset grouped results, otherwise any aggregated results in the next member which needs to use non-grouped data will be incorrect.
                if (this.Tablix.GroupedResults != null && !this.Tablix.Parents.Any(ri => ri.GetType() == typeof(Tablix)))
                {
                    this.Tablix.GroupedResults = null;
                    this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = null;                    
                }
            }

            sb.AppendLine("</tbody>");

            return sb.ToString();
        }

        private int FindAllSubMembers(TablixMember member, int currentCount)
        {
            if (member.TablixMembers.Any())
            {
                foreach (var submember in member.TablixMembers)
                {
                    currentCount = this.FindAllSubMembers(submember, currentCount);
                }
            }

            return currentCount + 1;
        }

        private int ProcessTablixMembers(
            StringBuilder sb, 
            List<IDictionary<string, object>> dataSetResults, 
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,            
            int currentRowIndx,
            PageBreak pageBreak,
            TablixHierarchyGroupStructure tablixHierarchyStructure)
        {
            var dsr = this.Tablix.DataSetReference.DataSet.DataSetResults;

            // If current tablix member is a group, then any <TablixMember> elements which are at the same level are classed as being related
            // to the current member. If the member is not a group in its own right, then any <TablixMember> elements at the same level are for other rows.

            if (tablixMember.TablixMemberSort != null && this.Tablix.DataSetReference != null && this.Tablix.DataSetReference.DataSet.DataSetResults != null)
            {
                // Order dataset results by expression.                    
                var fieldsIdx = tablixMember.TablixMemberSort.SortExpression.IndexOf("Fields!");
                var fieldEnd = tablixMember.TablixMemberSort.SortExpression.IndexOf('.', fieldsIdx);
                var fieldName = tablixMember.TablixMemberSort.SortExpression.Substring(fieldsIdx + 7, fieldEnd - (fieldsIdx + 7)).ToLower();
                var baseComparer = new TablixMemberSortComparer(fieldName, null);

                if (this.Tablix.DataSetReference?.DataSet?.DataSetResults != null)
                {
                    this.Tablix.DataSetReference.DataSet.DataSetResults = this.SortTablixMember(tablixMember, baseComparer, this.Tablix.DataSetReference.DataSet.DataSetResults.Order(baseComparer)).ToList();
                }                
            }
                        
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

                    this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = this.Tablix.GroupedResults.GroupBy(g => g[fieldName]).ToList();                    
                }
                else
                {
                    this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = this.Tablix.DataSetReference.DataSet.DataSetResults.GroupBy(g => g[fieldName]).ToList();
                }                
            }
            else if (this.Tablix.GroupedResults != null && (this.Tablix.Parents.Any(ri => ri.GetType() == typeof(Tablix)) || prevTablixMembers.Any(tm => tm.TablixMemberGroup != null && tm.TablixMemberGroup.GroupExpression != null)))
            {
                // Have we been provided with group results from further up the tree?                
                this.Tablix.DataSetReference.DataSet.GroupedDataSetResults = new List<IGrouping<object, IDictionary<string, object>>>() { this.Tablix.GroupedResults };                                
            }

            if (tablixMember.TablixMemberGroup != null && pageBreak == PageBreak.None)
            {
                pageBreak = tablixMember.TablixMemberGroup.PageBreak;
            }

            if (this.Tablix?.DataSetReference?.DataSet?.GroupedDataSetResults != null && tablixMember.TablixMemberGroup != null && tablixMember.TablixMemberGroup.GroupExpression != null)
            {
                // Starting a new grouping
                var after = 0;
                var groupedResults = this.Tablix.DataSetReference.DataSet.GroupedDataSetResults;

                tablixHierarchyStructure.TotalPages = groupedResults.Count;

                foreach (var groupedResult in groupedResults)
                {
                    this.Tablix.GroupedResults = groupedResult;

                    if (!this.Tablix.GroupedResultsKeys.Contains(groupedResult.Key.ToString().Replace(" ", "")))
                    {
                        this.Tablix.GroupedResultsKeys.Add(groupedResult.Key.ToString().Replace(" ", ""));
                    }

                    var before = currentRowIndx;
                    var prevMembersBeforeGroup = new List<TablixMember>(prevTablixMembers);
                    
                    prevTablixMembers.Add(tablixMember);

                    if (tablixMember.TablixMembers.Any())
                    {                        
                        if (tablixMember.TablixMembers.Any(tm => tm.TablixMemberGroup != null))
                        {
                            currentRowIndx = this.ProcessResults(currentRowIndx, tablixMember, prevTablixMembers, groupedResult, tablixHierarchyStructure, pageBreak, sb, dsr);
                        }

                        foreach (var childMember in tablixMember.TablixMembers)
                        {                                                       
                            currentRowIndx = this.ProcessTablixMembers(sb, dataSetResults, groupedResult, childMember, prevTablixMembers, currentRowIndx, pageBreak, tablixHierarchyStructure);
                        }                                                
                    }
                    else
                    {
                        currentRowIndx = this.ProcessResults(currentRowIndx, tablixMember, prevTablixMembers, groupedResult, tablixHierarchyStructure, pageBreak, sb, dsr);                        
                    }

                    this.Tablix.GroupedResultsKeys.Remove(groupedResult.Key.ToString());

                    prevTablixMembers = prevMembersBeforeGroup;
                    tablixHierarchyStructure.NewGroup();
                    tablixHierarchyStructure.CurrentGroupPage = tablixHierarchyStructure.CurrentGroupPage + 1;
                    after = currentRowIndx;
                    currentRowIndx = before;
                }

                currentRowIndx = after;
            }
            else if (tablixMember.TablixMembers.Any())
            {
                prevTablixMembers.Add(tablixMember);

                foreach (var childMember in tablixMember.TablixMembers)
                {                    
                    currentRowIndx = this.ProcessTablixMembers(sb, dataSetResults, groupResults, childMember, prevTablixMembers, currentRowIndx, pageBreak, tablixHierarchyStructure);                    
                }
            }
            else
            {
                currentRowIndx = this.ProcessResults(currentRowIndx, tablixMember, prevTablixMembers, groupResults, tablixHierarchyStructure, pageBreak, sb, dsr);                
            }
                        
            return currentRowIndx;
        }

        private int ProcessResults(
            int currentRowIndx,
            TablixMember tablixMember,
            List<TablixMember> prevTablixMembers,
            IGrouping<object, IDictionary<string, object>> groupResults,
            TablixHierarchyGroupStructure tablixHierarchyStructure,
            PageBreak pageBreak,
            StringBuilder sb,
            List<IDictionary<string, object>> dsr            
        )
        {
            if (this.TablixRows.Count <= currentRowIndx)
            {
                // The TablixRowHierarchy structure does not seem to be robust and tally up with the number of rows expected, either that or my understanding 
                // is wrong. For now putting this in to make sure it doesn't cry.
                return currentRowIndx;
            }

            var row = this.TablixRows[currentRowIndx];
            var newKey = this.Tablix.GroupedResultsKeys.Any() ? string.Join('-', this.Tablix.GroupedResultsKeys) : Guid.NewGuid().ToString().Split('-')[0];

            row.GroupedResults = groupResults;

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

            if (lastHeader != null &&
                lastHeader.TablixHeader != null &&
                (lastHeader.TablixHeader.ContainsRepeatExpression || row.ContainsRepeatExpression) &&
                !lastHeader.TablixHeader.ContainsAggregatorExpression &&
                !row.ContainsAggregatorExpression &&
                this.Tablix.DataSetReference != null &&
                this.Tablix.DataSetReference.DataSet.DataSetResults != null)
            {
                this.ProcessTablixRowHierarchyMember(groupResults, tablixMember, lastGroup, lastHeader, row, tablixHierarchyStructure, headersFoundInTablixMembers, pageBreak, newKey, sb, dsr);
            }
            else if (lastGroup != null &&
                     row.ContainsRepeatExpression &&
                     !row.ContainsAggregatorExpression &&
                     this.Tablix.DataSetReference != null &&
                     this.Tablix.DataSetReference.DataSet.DataSetResults != null

                )
            {
                this.ProcessTablixRowHierarchyMember(groupResults, tablixMember, lastGroup, lastHeader, row, tablixHierarchyStructure, headersFoundInTablixMembers, pageBreak, newKey, sb, dsr);
            }
            else
            {
                sb.AppendLine(
                    lastGroup != null ?
                    $"<tr height=\"{row.Height}\" data-grouped-result=\"true\">" :
                    $"<tr height=\"{row.Height}\" data-grouped-result=\"false\">"
                );

                if (!tablixHierarchyStructure.InsertedKey)
                {
                    if (headersFoundInTablixMembers.Any())
                    {
                        foreach (var header in headersFoundInTablixMembers)
                        {
                            if ((header.TablixHeader.ContainsRepeatExpression && !tablixHierarchyStructure.InsertedKey) || !header.TablixHeader.ContainsRepeatExpression)
                            {
                                sb.AppendLine(header.TablixHeader.Build());
                            }
                        }
                    }
                    else if (lastHeader != null && lastHeader.TablixHeader != null && ((lastHeader.TablixHeader.ContainsRepeatExpression && !tablixHierarchyStructure.InsertedKey) || !lastHeader.TablixHeader.ContainsRepeatExpression))
                    {
                        sb.AppendLine(lastHeader.TablixHeader.Build());
                    }
                }
                
                if (!tablixMember.Hidden || (tablixMember.Hidden && this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tablixHierarchyStructure.KeyGuid || ti == tablixMember.ToggleItem)))
                {
                    tablixMember.Hidden = false;
                    sb.AppendLine(row.Build());
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
            PageBreak pageBreak,
            string newKey,
            StringBuilder sb,
            List<IDictionary<string, object>> dsr
            )
        {
            if (groupResults != null)
            {
                tablixHierarchyStructure.KeyGuid = newKey;

                var groupRowCount = this.FindAllSubMembers(lastGroup, 0);
                                
                // We are displaying grouped results.                                
                foreach (var result in groupResults)
                {
                    row.Values = result;

                    var dataPageBreak = "";
                    var dataGroupResultsCount = $"data-tablepages=\"{tablixHierarchyStructure.TotalPages}\"";
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
                    if (!tablixHierarchyStructure.InsertedKey)
                    {
                        sb.AppendLine($"<tr height=\"{row.Height}\" data-grouped-result=\"true\" data-rowspan-start {dataPageBreak} {dataGroupResultsCount} {dataPageNumber} data-row-key=\"{newKey}\">");

                        if (headersFoundInTablixMembers.Any())
                        {
                            foreach (var header in headersFoundInTablixMembers)
                            {
                                header.Values = result;
                                header.TablixHeader.IsKey = true;
                                sb.AppendLine(header.TablixHeader.Build());
                            }
                        }
                        else if (lastHeader != null)
                        {
                            lastHeader.Values = result;
                            lastHeader.TablixHeader.IsKey = true;
                            sb.AppendLine(lastHeader.TablixHeader.Build());
                        }

                        if (!tablixMember.Hidden || (tablixMember.Hidden && this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tablixHierarchyStructure.KeyGuid || ti == tablixMember.ToggleItem)))
                        {
                            tablixMember.Hidden = false;
                            sb.AppendLine(row.Build());
                        }
                                                
                        tablixHierarchyStructure.InsertedKey = true;

                        sb.AppendLine("</tr>");
                    }
                    else
                    {
                        if (!tablixMember.Hidden || (tablixMember.Hidden && this.Tablix.Report.ToggleItemRequests.Any(ti => ti == tablixHierarchyStructure.KeyGuid || ti == tablixMember.ToggleItem)))
                        {
                            tablixMember.Hidden = false;
                            sb.AppendLine($"<tr height=\"{row.Height}\" data-grouped-result=\"true\" data-row-key=\"{tablixHierarchyStructure.KeyGuid}\">");
                            sb.AppendLine(row.Build());
                            sb.AppendLine("</tr>");
                        }                                                
                    }                    

                    // I don't know if this is right or just plain hacky. If the current TablixMember is a group and has children then we know that this instance
                    // just needs to be rendered once. However, if the TablixMember is a group and has a header that should be repeated then we should loop over the results.
                    // Honestly, at this stage I'm just making this stuff up.
                    if (tablixMember.ContainsReportItemWithSubGroup || (tablixMember.IsGroupHasMember && (tablixMember.TablixHeader == null || (tablixMember.TablixHeader != null && !tablixMember.TablixHeader.ContainsRepeatExpression))))
                    {
                        break;
                    }
                }
            }
            else
            {
                foreach (var result in dsr)
                {
                    row.Values = result;

                    sb.AppendLine($"<tr height=\"{row.Height}\" data-grouped-result=\"false\">");
                    sb.AppendLine(row.Build());
                    sb.AppendLine("</tr>");
                }
            }
        }
                
        /// <summary>
        /// Recursive method which will find sub sort tablix members and apply the <see cref="TablixMemberSortComparer"/> class.
        /// </summary>
        /// <param name="tablixMember">The current tablix member, we expect this to have a sort expression.</param>
        /// <param name="baseComparer">The base comparer which may have parents.</param>
        /// <param name="dataSet">The current dataset which will have been ordered at least once previously.</param>
        /// <returns>An <see cref="IOrderedEnumerable{TElement}"/> dataset.</returns>
        private IOrderedEnumerable<IDictionary<string, object>> SortTablixMember(TablixMember tablixMember, TablixMemberSortComparer baseComparer, IOrderedEnumerable<IDictionary<string, object>> dataSet)
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
    }

    public class TablixColumn
    {
        public string Width { get; set; }
        public TablixBody Body { get; set; }

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

    public class TablixRow
    {
        public string Height { get; set; }
        public List<TablixCell> TablixCells { get; set; }
        public TablixBody Body { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public bool ContainsAggregatorExpression { get; set; }
        public dynamic Values { get; set; }
        public IGrouping<object, IDictionary<string, object>> GroupedResults { get; set; }

        internal TablixRow(TablixBody body, XElement row)
        {
            this.Body = body;
            this.Height = row.Element(this.Body.Tablix.Report.Namespace + "Height")?.Value;
            this.TablixCells = new List<TablixCell>();

            var cells = row.Elements(this.Body.Tablix.Report.Namespace + "TablixCells").Elements(this.Body.Tablix.Report.Namespace + "TablixCell");

            if (cells != null)
            {
                foreach (var c in cells)
                {
                    this.TablixCells.Add(new TablixCell(this, c, this.Body.Tablix.Report));

                    if (!this.ContainsRepeatExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        this.ContainsRepeatExpression = !CountParser.CountRegex.IsMatch(c.Value) && FieldParser.FieldRegex.IsMatch(c.Value);
                    }

                    if (!this.ContainsAggregatorExpression && !string.IsNullOrEmpty(c.Value))
                    {
                        // TODO: Add other aggregator functions.
                        this.ContainsAggregatorExpression = CountParser.CountRegex.IsMatch(c.Value);
                    }
                }
            }
        }

        public string Build()
        {
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

                    if (this.TablixCells[i].TablixCellContent.Count > 0)
                    {
                        sb.AppendLine(emptyCells > 0 ? $"<td colspan=\"{emptyCells + 1}\">" : "<td>");
                    }

                    for (var j = 0; j < this.TablixCells[i].TablixCellContent.Count; j++)
                    {
                        var content = this.TablixCells[i].TablixCellContent[j];
                                                                        
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
                                if (this.Body.Tablix.Report.UserProvidedParameters.Any(rp => rp.Name == p.Name))
                                {
                                    finalUserParamsForSubReport.Add(this.Body.Tablix.Report.UserProvidedParameters.First(rp => rp.Name == p.Name));
                                }
                                // Try to retrieve the parameter from the tablix row.
                                else
                                {
                                    var expressionParser = new ExpressionParser();
                                    var dataSetResults = this.GroupedResults?.Select(r => r).ToList() ?? this.Body.Tablix.DataSetReference?.DataSet?.DataSetResults;
                                    var parsedValue = expressionParser.ParseTablixExpressionString(p.Value, dataSetResults, (IDictionary<string, object>)this.Values, null, null);

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

                            sb.AppendLine(layoutProvider.PublishReportOutput(srRdl, finalUserParamsForSubReport, this.Body.Tablix.Report.ToggleItemRequests).GetAwaiter().GetResult().Value);
                        }                        
                        else
                        {
                            sb.AppendLine(content.Build(this.Body.Tablix));
                        }
                    }
                    
                    if (this.TablixCells[i].TablixCellContent.Count > 0)
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
            return sb.ToString();
        }
    }

    public class TablixCell
    {
        public List<ReportItem> TablixCellContent { get; set; }
        public TablixRow Row { get; set; }
        public TablixHeader Header { get; set; }        
        public ReportRDL Report { get; set; }

        internal TablixCell(TablixRow row, XElement cell, ReportRDL report)        
        {
            this.Row = row;
            this.Report = report;
            this.TablixCellContent = new List<ReportItem>();

            var cellContents = cell.Elements(this.Report.Namespace + "CellContents");
            var reportItems = ReportItem.ParseElements(cellContents, report, this.Row.Body.Tablix.DataSets, this, this.Row.Body.Tablix);

            this.TablixCellContent.AddRange(reportItems);
        }

        internal TablixCell(TablixHeader tablixHeader, XElement cell, ReportRDL report)           
        {
            this.Header = tablixHeader;
            this.Report = report;
            this.TablixCellContent = new List<ReportItem>();

            var cellContents = cell.Elements(this.Report.Namespace + "CellContents");

            if (cellContents != null)
            {
                foreach (var c in cellContents)
                {
                    var textboxes = c.Elements(this.Report.Namespace + "Textbox");

                    if (textboxes != null)
                    {
                        foreach (var textbox in textboxes)
                        {
                            this.TablixCellContent.Add(new Textbox(this, textbox, this.Header.TablixMember.TablixHierarchy.Tablix.DataSets, this.Report, this.Header.TablixMember.TablixHierarchy.Tablix));
                        }
                    }
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

            var tablixMemberElements = element.Elements(this.Tablix.Report.Namespace + "TablixMembers").Elements(this.Tablix.Report.Namespace + "TablixMember");

            foreach (var member in tablixMemberElements)
            {
                this.TablixMembers.Add(new TablixMember(member, this));
            }
        }

        public TablixHierarchy(XElement element, TablixBody tablixBody)
        {
            this.Tablix = tablixBody.Tablix;
            this.TablixMembers = new List<TablixMember>();

            var tablixMemberElements = element.Elements(this.Tablix.Report.Namespace + "TablixMembers").Elements(this.Tablix.Report.Namespace + "TablixMember");

            foreach (var member in tablixMemberElements)
            {
                this.TablixMembers.Add(new TablixMember(member, this));
            }
        }
    }

    public class TablixMember
    {
        public string Id { get; set; }
        public TablixHierarchy TablixHierarchy { get; set; }
        public TablixMemberGroup TablixMemberGroup { get; set; }
        public TablixMemberSort TablixMemberSort { get; set; }
        public TablixHeader TablixHeader { get; set; }
        public List<TablixMember> TablixMembers { get; set; }
        public dynamic Values { get; set; }
        public string KeepWithGroup { get; set; }
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }
        public bool RepeatOnNewPage { get; set; }
        public bool KeepTogether { get; set; }
        public bool IsGroupHasMember => this.TablixMemberGroup != null && this.TablixMembers.Any();
        public IGrouping<object, IDictionary<string, object>> GroupedResults { get; set; }
        public bool ContainsReportItemWithSubGroup { get; set; }

        public TablixMember(XElement element, TablixHierarchy tablixHierarchy)
        {
            this.Id = Guid.NewGuid().ToString().Split('-')[0];
            this.TablixHierarchy = tablixHierarchy;
            this.TablixMembers = new List<TablixMember>();

            var tablixGroup = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Group");
            var tablixSort = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "SortExpressions")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "SortExpression");
            var tablixHeader = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "TablixHeader");
            var tablixMemberElements = element.Elements(this.TablixHierarchy.Tablix.Report.Namespace + "TablixMembers")?.Elements(this.TablixHierarchy.Tablix.Report.Namespace + "TablixMember");
            
            var hidden = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Visibility")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Hidden")?.Value;
            var toggleItem = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "Visibility")?.Element(this.TablixHierarchy.Tablix.Report.Namespace + "ToggleItem")?.Value;

            this.KeepWithGroup = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "KeepWithGroup")?.Value;
            this.KeepTogether = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "KeepTogether")?.Value == "true";
            this.RepeatOnNewPage = element.Element(this.TablixHierarchy.Tablix.Report.Namespace + "RepeatOnNewPage")?.Value == "true";
            this.ContainsReportItemWithSubGroup = this.TablixHierarchy.Tablix.XElement.Element(this.TablixHierarchy.Tablix.Report.Namespace + "TablixBody").Descendants(this.TablixHierarchy.Tablix.Report.Namespace + "Group").Any();

            if (hidden != null && hidden == "true")
            {
                this.Hidden = true;
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
                this.TablixHeader = new TablixHeader(this, tablixHeader);
            }
                
            if (tablixMemberElements != null)
            {
                var membersToProcess = new List<TablixMember>();
                
                foreach (var member in tablixMemberElements)
                {
                    if (!member.IsEmpty)
                    {
                        membersToProcess.Add(new TablixMember(member, this.TablixHierarchy));
                    }                                        
                }

                if (membersToProcess.Any(tm => tm.TablixMemberGroup != null))
                {
                    // Merge details from the <TablixMember> elements at same level into the tablix group member.
                    var configsToMerge = membersToProcess.Where(tm => tm.TablixMemberGroup == null);
                    var groupMember = membersToProcess.Where(tm => tm.TablixMemberGroup != null).First();

                    foreach (var member in configsToMerge)
                    {
                        groupMember.KeepTogether = member.KeepTogether;
                        groupMember.RepeatOnNewPage = member.RepeatOnNewPage;
                        groupMember.KeepWithGroup = member.KeepWithGroup;
                        // Other configs?
                    }

                    this.TablixMembers.Add(groupMember);
                }
                else
                {
                    // Treat all <TablixMember> elements individually.
                    foreach (var member in membersToProcess)
                    {
                        this.TablixMembers.Add(member);
                    }
                }
            }    
                        
            if (this.Hidden)
            {
                this.TablixHierarchy.Tablix.Report.HiddenTablixMembers.Add(this);
            }
        }                
    }

    public class TablixHeader
    {
        public TablixMember TablixMember { get; set; }
        public string Size { get; set; }
        public TablixCell TablixHeaderContent { get; set; }
        public bool ContainsRepeatExpression { get; set; }
        public bool ContainsAggregatorExpression { get; set; }        
        public bool IsKey { get; set; }        
        public IGrouping<object, IDictionary<string, object>> GroupedResults { get; set; }

        public TablixHeader(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;
            this.Size = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "Size")?.Value;
            this.TablixHeaderContent = new TablixCell(this, element, this.TablixMember.TablixHierarchy.Tablix.Report);

            // TODO: Add other aggregator functions.
            this.ContainsAggregatorExpression = CountParser.CountRegex.IsMatch(element?.Value ?? string.Empty);
            this.ContainsRepeatExpression = !this.ContainsAggregatorExpression && FieldParser.FieldRegex.IsMatch(element?.Value ?? string.Empty);            
        }

        public string Build()
        {
            var sb = new StringBuilder();

            if (this.TablixHeaderContent.TablixCellContent.Count > 0)
            {
                sb.AppendLine(this.IsKey ? "<td data-group-key>" : "<td>");
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

    public class TablixMemberSort
    {        
        public string SortExpression { get; set; }
        public TablixMember TablixMember { get; set; }

        public TablixMemberSort(TablixMember tablixMember, XElement element)
        {
            this.TablixMember = tablixMember;            
            this.SortExpression = element.Element(this.TablixMember.TablixHierarchy.Tablix.Report.Namespace + "Value")?.Value;
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
        Sum,
        Field,
        Add,
        Subtract,
        Multiply,
        Divide,
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
        And,
        Not,
        Or,
        Xor,
        AndAlso,
        OrElse,
        Left,
        MonthName
    }
}
