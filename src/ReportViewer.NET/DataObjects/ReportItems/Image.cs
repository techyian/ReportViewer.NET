using System;
using System.Collections.Generic;
using System.Linq;
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
                switch (this.EmbeddedImage.MimeType)
                {
                    // TODO: Handle other mime types.
                    case "image/jpeg":
                        return $"<img class=\"img\" {Style?.Build()} data-toggle=\"{this.ToggleItem}\" src=\"data:image/jpeg;base64, {this.EmbeddedImage.ImageData}\" />";
                }
            }

            return string.Empty;
        }
    }
}
