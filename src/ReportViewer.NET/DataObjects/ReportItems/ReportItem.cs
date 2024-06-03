using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{    
    public abstract class ReportItem
    {
        public static XNamespace Namespace = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";

        public string Name { get; set; }        
        public Style Style { get; set; }
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Hidden { get; set; }
        public string ToggleItem { get; set; }
        public bool DoesToggle { get; set; }
        
        public ReportItem(XElement element)
        {
            var topValue = element.Element(Namespace + "Top")?.Value;
            var leftValue = element.Element(Namespace + "Left")?.Value;
            var widthValue = element.Element(Namespace + "Width")?.Value;
            var heightValue = element.Element(Namespace + "Height")?.Value;

            if (!string.IsNullOrEmpty(topValue) && double.TryParse(topValue.Substring(0, topValue.Length - 2), out var top))
            {
                Top = top;
            }

            if (!string.IsNullOrEmpty(leftValue) && double.TryParse(leftValue.Substring(0, leftValue.Length - 2), out var left))
            {
                Left = left;
            }

            if (!string.IsNullOrEmpty(widthValue) && double.TryParse(widthValue.Substring(0, widthValue.Length - 2), out var width))
            {
                Width = width;
            }

            if (!string.IsNullOrEmpty(heightValue) && double.TryParse(heightValue.Substring(0, heightValue.Length - 2), out var height))
            {
                Height = height;
            }

            this.Style = new Style(element.Element(Namespace + "Style"));
            this.Style.Top = topValue;
            this.Style.Left = leftValue;
            this.Style.Height = heightValue;
            this.Style.Width = widthValue;

            this.Hidden = this.Style.Hidden = element.Element(Namespace + "Visibility")?.Element(Namespace + "Hidden")?.Value == "true";
            this.ToggleItem = element.Element(Namespace + "Visibility")?.Element(Namespace + "ToggleItem")?.Value;
        }

        public abstract string Build();
    }

    public class ReportRow
    {
        public double MaxTop { get; set; }
        public double MaxHeight { get; set; }
        public double MaxLeft { get; set; }
        public double MaxWidth { get; set; }
        public double TotalWidth { get; set; }
        public double TotalHeight { get; set; }
        public List<ReportItem> RowItems { get; set; } = new List<ReportItem>();
    }
}
