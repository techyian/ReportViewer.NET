namespace ReportViewer.NET.DataObjects
{
    public class DataSource
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }

        public DataSource(string name, string connectionString)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
        }
    }
}
