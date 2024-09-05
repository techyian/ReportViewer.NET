namespace ReportViewer.NET.DataObjects
{
    public class TablixHierarchyGroupStructure
    {
        public bool InsertedKey { get; set; }
        public string KeyGuid { get; set; }
        public int CurrentGroupPage { get; set; }  
        public int CurrentRowIndex { get; set; }
        public int TotalPages { get; set; }

        public void NewGroup()
        {
            this.InsertedKey = false;
            this.KeyGuid = "";
        }
    }
}
