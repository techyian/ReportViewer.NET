namespace ReportViewer.NET.DataObjects
{
    public class DataSource
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string DataSourceReference { get; set; }

        public DataSource(string name, string connectionString, string dataSourceReference)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
            this.DataSourceReference = dataSourceReference;
        }
    }
}
