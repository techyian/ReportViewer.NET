using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Image : ReportItem
    {
        public string Source { get; private set; }
        // This is the "name" of the image we're looking for
        public string Value { get; private set; }
        public string Sizing { get; private set; }
        public string MIMEType { get; private set; }
        public EmbeddedImage EmbeddedImage { get; private set; }
                
        public Image(XElement image, ReportRDL report, ReportItem parent)
            : base(image, report, parent)
        {            
            this.Source = image.Element(report.Namespace + "Source")?.Value;
            this.Value = image.Element(report.Namespace + "Value")?.Value;
            this.Sizing = image.Element(report.Namespace + "Sizing")?.Value;
            this.MIMEType = image.Element(report.Namespace + "MIMEType")?.Value;

            // TODO: Handle other sources?             
            this.EmbeddedImage = report.EmbeddedImages?.FirstOrDefault(i => i.Name == this.Value);
        }

        public override string Build(ReportItem parent)
        {
            this.NestedCopy(parent, this);

            // TODO: Handle sizings?            
            var sb = new StringBuilder();
            
            if (!this.Hidden || (this.Hidden && this.Report.ToggleItemRequests.Contains(this.ToggleItem)))
            {               
                this.Hidden = false;
                this.Style.Hidden = false;

                if (this.EmbeddedImage != null)
                {
                    sb.AppendLine(this.BuildEmbeddedImage());
                }
                else
                {
                    sb.AppendLine(this.BuildDatabaseImage(parent));
                }
            }

            this.Values = null;

            return sb.ToString();
        }

        private string BuildEmbeddedImage()
        {
            switch (this.EmbeddedImage.MimeType)
            {
                // TODO: Handle other mime types.
                case "image/jpeg":
                    return $"<img class=\"reportviewer-image img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/jpeg;base64, {this.EmbeddedImage.ImageData}\" />";                    
                case "image/png":
                    return $"<img class=\"reportviewer-image img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/png;base64, {this.EmbeddedImage.ImageData}\" />";
                case "image/bmp":
                    return $"<img class=\"reportviewer-image img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/bmp;base64, {this.EmbeddedImage.ImageData}\" />";
                case "image/gif":
                    return $"<img class=\"reportviewer-image img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/gif;base64, {this.EmbeddedImage.ImageData}\" />";
            }

            return string.Empty;
        }

        private string BuildDatabaseImage(ReportItem parent)
        {
            var dataSetResults = this.GroupedResults?.Select(r => r).ToList() ?? this.DataSetReference?.DataSet?.DataSetResults;
            var parsedValue = this.Report.Parser.ParseReportExpressionString(this.Value, dataSetResults, this.Values, this.CurrentRowNumber, this.DataSets, this.DataSetReference?.DataSet, null);
            var b64 = string.Empty;

            // TODO: Will this ever not be a byte[]?
            if (parsedValue is byte[])
            {
                b64 = Convert.ToBase64String((byte[])parsedValue);
            }
                        
            switch (this.MIMEType)
            {
                // TODO: Handle other mime types.
                case "image/jpeg":
                    return $"<img class=\"img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/jpeg;base64, {b64}\" />";
                case "image/png":
                    return $"<img class=\"img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/png;base64, {b64}\" />";
            }

            return string.Empty;
        }
    }
}
