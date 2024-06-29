using System.Text;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Style
    {
        public string TextAlign { get; set; }
        public Border Border { get; set; }
        public Border BorderBottom { get; set; }
        public Border TopBorder { get; set; }
        public Border LeftBorder { get; set; }
        public Border RightBorder { get; set; }
        public string PaddingLeft { get; set; }
        public string PaddingRight { get; set; }
        public string PaddingTop { get; set; }
        public string PaddingBottom { get; set; }
        public string BackgroundColor { get; set; }
        public string BackgroundColorExpressionValue { get; set; }
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

        public Style(XElement style, ReportRDL report)
        {
            this.TextAlign = style?.Element(report.Namespace + "TextAlign")?.Value;

            var border = style?.Element(report.Namespace + "Border");
            var borderBottom = style?.Element(report.Namespace + "BorderBottom");
            var topBorder = style?.Element(report.Namespace + "TopBorder");
            var leftBorder = style?.Element(report.Namespace + "LeftBorder");
            var rightBorder = style?.Element(report.Namespace + "RightBorder");

            if (border != null)
            {
                this.Border = new Border
                {
                    Color = border.Element(report.Namespace + "Color")?.Value,
                    Style = border.Element(report.Namespace + "Style")?.Value,
                    Width = border.Element(report.Namespace + "Width")?.Value ?? "1px"
                };
            }

            if (borderBottom != null)
            {
                this.BorderBottom = new Border
                {
                    Color = borderBottom.Element(report.Namespace + "Color")?.Value,
                    Style = borderBottom.Element(report.Namespace + "Style")?.Value,
                    Width = borderBottom.Element(report.Namespace + "Width")?.Value ?? "1px"
                };
            }

            if (topBorder != null)
            {
                this.TopBorder = new Border
                {
                    Color = topBorder.Element(report.Namespace + "Color")?.Value,
                    Style = topBorder.Element(report.Namespace + "Style")?.Value,
                    Width = topBorder.Element(report.Namespace + "Width")?.Value ?? "1px"
                };
            }

            if (leftBorder != null)
            {
                this.LeftBorder = new Border
                {
                    Color = leftBorder.Element(report.Namespace + "Color")?.Value,
                    Style = leftBorder.Element(report.Namespace + "Style")?.Value,
                    Width = leftBorder.Element(report.Namespace + "Width")?.Value ?? "1px"
                };
            }

            if (rightBorder != null)
            {
                this.RightBorder = new Border
                {
                    Color = rightBorder.Element(report.Namespace + "Color")?.Value,
                    Style = rightBorder.Element(report.Namespace + "Style")?.Value,
                    Width = rightBorder.Element(report.Namespace + "Width")?.Value ?? "1px"
                };
            }

            this.PaddingLeft = style?.Element(report.Namespace + "PaddingLeft")?.Value;
            this.PaddingRight = style?.Element(report.Namespace + "PaddingRight")?.Value;
            this.PaddingTop = style?.Element(report.Namespace + "PaddingTop")?.Value;
            this.PaddingBottom = style?.Element(report.Namespace + "PaddingBottom")?.Value;
            this.BackgroundColor = style?.Element(report.Namespace + "BackgroundColor")?.Value;
            this.VerticalAlign = style?.Element(report.Namespace + "VerticalAlign")?.Value;
            this.Top = style?.Element(report.Namespace + "Top")?.Value;
            this.Left = style?.Element(report.Namespace + "Left")?.Value;
            this.Height = style?.Element(report.Namespace + "Height")?.Value;
            this.Width = style?.Element(report.Namespace + "Width")?.Value;
            this.ZIndex = style?.Element(report.Namespace + "ZIndex")?.Value;
            this.FontFamily = style?.Element(report.Namespace + "FontFamily")?.Value;
            this.FontWeight = style?.Element(report.Namespace + "FontWeight")?.Value;
            this.FontSize = style?.Element(report.Namespace + "FontSize")?.Value;
            this.Color = style?.Element(report.Namespace + "Color")?.Value;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append(!string.IsNullOrEmpty(this.TextAlign) ? $"text-align: {this.TextAlign.ToLower()};" : "");

            if (this.Border != null)
            {
                sb.Append(this.Border.Style == "None" ? "" : $"border: {this.Border.Width} solid {this.Border.Color};");
            }

            if (this.BorderBottom != null)
            {
                sb.Append(this.BorderBottom.Style == "None" ? "" : $"border-bottom: {this.BorderBottom.Width} solid {this.BorderBottom.Color};");
            }

            if (this.TopBorder != null)
            {
                sb.Append(this.TopBorder.Style == "None" ? "" : $"border-top: {this.TopBorder.Width} solid {this.TopBorder.Color};");
            }

            if (this.LeftBorder != null)
            {
                sb.Append(this.LeftBorder.Style == "None" ? "" : $"border-left: {this.LeftBorder.Width} solid {this.LeftBorder.Color};");
            }

            if (this.RightBorder != null)
            {
                sb.Append(this.RightBorder.Style == "None" ? "" : $"border-right: {this.RightBorder.Width} solid {this.RightBorder.Color};");
            }

            sb.Append(!string.IsNullOrEmpty(this.PaddingLeft) ? $"padding-left: {this.PaddingLeft};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingRight) ? $"padding-right: {this.PaddingRight};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingTop) ? $"padding-top: {this.PaddingTop};" : "");
            sb.Append(!string.IsNullOrEmpty(this.PaddingBottom) ? $"padding-bottom: {this.PaddingBottom};" : "");
            
            if (!string.IsNullOrEmpty(this.BackgroundColorExpressionValue))
            {
                sb.Append($"background-color: {this.BackgroundColorExpressionValue};");
            }
            else if (!string.IsNullOrEmpty(this.BackgroundColor))
            {
                sb.Append($"background-color: {this.BackgroundColor};");
            }
            
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
