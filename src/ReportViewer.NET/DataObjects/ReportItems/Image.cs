using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Image : ReportItem
    {
        public string Source { get; set; }
        // This is the "name" of the image we're looking for
        public string Value { get; set; }
        public string Sizing { get; set; }
        public EmbeddedImage EmbeddedImage { get; set; }

        public Image(XElement image, IEnumerable<EmbeddedImage> embeddedImages, ReportRDL report)
            : base(image, report)
        {
            this.Style.Top = "";
            this.Style.Left = "";

            this.Source = image.Element(report.Namespace + "Source")?.Value;
            this.Value = image.Element(report.Namespace + "Value")?.Value;
            this.Sizing = image.Element(report.Namespace + "Sizing")?.Value;

            // TODO: Handle other sources? 
            this.EmbeddedImage = embeddedImages.FirstOrDefault(i => i.Name == this.Value);
        }

        public override string Build()
        {
            // TODO: Handle sizings?
            if (this.EmbeddedImage != null)
            {
                var sb = new StringBuilder();
                var isLastItem = false;
                
                for (var i = 0; i < this.ReportRow.RowItems.Count(); i++)
                {
                    if (object.ReferenceEquals(this.ReportRow.RowItems[i], this) && i == this.ReportRow.RowItems.Count() - 1)
                    {
                        isLastItem = true;
                    }
                }
                                
                // Making some decisions here as to the positioning of images. This library is not using absolute positioning
                // on elements, and images in particular fall foul to this depending on where they are positioned. Making the decision
                // to left/center/right align based on the width of the row and the left property. If the left property is <= 25% of the width
                // of the row then left align, <= 50% center align, <= 75 right align.
                var align = "justify-content: start;";
                var div = this.ReportRow.RowWidth / 3;

                if (this.Left > div && this.Left <= div * 2)
                {
                    align = "justify-content: middle;";
                }

                if (this.Left >= div * 3)
                {
                    align = "justify-content: end;";
                }

                if (isLastItem)
                {
                    sb.AppendLine($"<div style=\"display:inline-flex;width:{this.ReportRow.RowWidth - this.Left}mm;{align}\">");
                }
                else
                {
                    sb.AppendLine($"<div style=\"display:inline-flex;width:auto;{align}\">");
                }
                
                switch (this.EmbeddedImage.MimeType)
                {
                    // TODO: Handle other mime types.
                    case "image/jpeg":
                        sb.AppendLine($"<img class=\"img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/jpeg;base64, {this.EmbeddedImage.ImageData}\" />");
                        break;
                    case "image/png":
                        sb.AppendLine($"<img class=\"img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/png;base64, {this.EmbeddedImage.ImageData}\" />");
                        break;
                }

                sb.AppendLine("</div>");

                return sb.ToString();
            }

            return string.Empty;
        }
    }
}
