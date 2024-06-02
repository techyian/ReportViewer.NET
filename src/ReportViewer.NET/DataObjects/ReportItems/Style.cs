using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Style
    {
        public string TextAlign { get; set; }
        public Border Border { get; set; }
        public Border BorderBottom { get; set; }
        public string PaddingLeft { get; set; }
        public string PaddingRight { get; set; }
        public string PaddingTop { get; set; }
        public string PaddingBottom { get; set; }
        public string BackgroundColor { get; set; }
        public string VerticalAlign { get; set; }
        public string Top { get; set; }
        public string Left { get; set; }
        public string Height { get; set; }
        public string Width { get; set; }
        public string ZIndex { get; set; }
        public string FontFamily { get; set; }
        public string FontWeight { get; set; }
        public string FontSize { get; set; }
        public string Color { get; set; }    
        public bool Hidden { get; set; }
        
        public Style()
        {

        }

        public Style(XElement style)
        {
            this.TextAlign = style?.Element(ReportItem.Namespace + "TextAlign")?.Value;

            var border = style?.Element(ReportItem.Namespace + "Border");
            var borderBottom = style?.Element(ReportItem.Namespace + "BorderBottom");

            if (border != null)
            {
                this.Border = new Border
                {
                    Color = border.Element(ReportItem.Namespace + "Color")?.Value,
                    Style = border.Element(ReportItem.Namespace + "Style")?.Value,
                    Width = border.Element(ReportItem.Namespace + "Width")?.Value
                };
            }

            if (borderBottom != null)
            {
                this.BorderBottom = new Border
                {
                    Color = borderBottom.Element(ReportItem.Namespace + "Color")?.Value,
                    Style = borderBottom.Element(ReportItem.Namespace + "Style")?.Value,
                    Width = borderBottom.Element(ReportItem.Namespace + "Width")?.Value
                };
            }

            this.PaddingLeft = style?.Element(ReportItem.Namespace + "PaddingLeft")?.Value;
            this.PaddingRight = style?.Element(ReportItem.Namespace + "PaddingRight")?.Value;
            this.PaddingTop = style?.Element(ReportItem.Namespace + "PaddingTop")?.Value;
            this.PaddingBottom = style?.Element(ReportItem.Namespace + "PaddingBottom")?.Value;
            this.BackgroundColor = style?.Element(ReportItem.Namespace + "BackgroundColor")?.Value;
            this.VerticalAlign = style?.Element(ReportItem.Namespace + "VerticalAlign")?.Value;
            this.Top = style?.Element(ReportItem.Namespace + "Top")?.Value;
            this.Left = style?.Element(ReportItem.Namespace + "Left")?.Value;
            this.Height = style?.Element(ReportItem.Namespace + "Height")?.Value;
            this.Width = style?.Element(ReportItem.Namespace + "Width")?.Value;
            this.ZIndex = style?.Element(ReportItem.Namespace + "ZIndex")?.Value;
            this.FontFamily = style?.Element(ReportItem.Namespace + "FontFamily")?.Value;
            this.FontWeight = style?.Element(ReportItem.Namespace + "FontWeight")?.Value;
            this.FontSize = style?.Element(ReportItem.Namespace + "FontSize")?.Value;
            this.Color = style?.Element(ReportItem.Namespace + "Color")?.Value;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append(!string.IsNullOrEmpty(this.TextAlign) ? $"text-align: {this.TextAlign.ToLower()};" : "");

            if (this.Border != null)
            {
                sb.Append(this.Border.Style == "None" ? "border: none;" : $"border: {this.Border.Width} solid {this.Border.Color};");
            }

            if (this.BorderBottom != null)
            {
                sb.Append(this.BorderBottom.Style == "None" ? "border: none;" : $"border-bottom: {this.BorderBottom.Width} solid {this.BorderBottom.Color};");
            }

            sb.Append(!string.IsNullOrEmpty(this.PaddingLeft) ? $"padding-left: {this.PaddingLeft};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingRight) ? $"padding-right: {this.PaddingRight};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingTop) ? $"padding-top: {this.PaddingTop};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingBottom) ? $"padding-bottom: {this.PaddingBottom};" : "");
            sb.Append(!string.IsNullOrEmpty(this.BackgroundColor) ? $"background-color: {this.BackgroundColor};" : "");

            if (!string.IsNullOrEmpty(this.VerticalAlign))
            {
                sb.Append(!string.IsNullOrEmpty(this.VerticalAlign) ? $"vertical-align: {this.VerticalAlign};" : "");

                switch(this.VerticalAlign)
                {
                    case "Top":
                        sb.Append("align-items: start;");
                        break;
                    case "Middle":
                        sb.Append("align-items: center;");
                        break;
                    case "Bottom":
                        sb.Append("align-items: end;");
                        break;
                }
            }
                        
            sb.Append(!string.IsNullOrEmpty(this.Top) ? $"top: {this.Top};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Left) ? $"left: {this.Left};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Height) ? $"height: {this.Height};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Width) ? $"width: {this.Width};" : "");
            sb.Append(!string.IsNullOrEmpty(this.ZIndex) ? $"z-index: {this.ZIndex};" : "");
            sb.Append(!string.IsNullOrEmpty(this.FontFamily) ? $"font-family: {this.FontFamily};" : "");
            sb.Append(!string.IsNullOrEmpty(this.FontWeight) ? $"font-weight: {this.FontWeight};" : "");
            sb.Append(!string.IsNullOrEmpty(this.FontSize) ? $"font-size: {this.FontSize};" : "");
            sb.Append(!string.IsNullOrEmpty(this.Color) ? $"color: {this.Color};" : "");
            sb.Append(this.Hidden ? $"display: none;" : "");

            sb.Append("\"");

            return sb.ToString();
        }
    }

    public class Border
    {
        public string Style { get; set; } = "None";
        public string Color { get; set; } = "transparent";
        public string Width { get; set; } = "0px";
    }
}
