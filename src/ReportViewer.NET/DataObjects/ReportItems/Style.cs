using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects.ReportItems
{
    public class Style
    {
        public string TextAlign { get; set; } = "Left";
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
        public string Color { get; set; }

        public Style()
        {

        }

        public Style(XElement style)
        {
            TextAlign = style?.Element(ReportItem.Namespace + "TextAlign")?.Value;

            var border = style?.Element(ReportItem.Namespace + "Border");
            var borderBottom = style?.Element(ReportItem.Namespace + "BorderBottom");

            if (border != null)
            {
                Border = new Border
                {
                    Color = border.Element(ReportItem.Namespace + "Color")?.Value,
                    Style = border.Element(ReportItem.Namespace + "Style")?.Value,
                    Width = border.Element(ReportItem.Namespace + "Width")?.Value
                };
            }

            if (borderBottom != null)
            {
                BorderBottom = new Border
                {
                    Color = borderBottom.Element(ReportItem.Namespace + "Color")?.Value,
                    Style = borderBottom.Element(ReportItem.Namespace + "Style")?.Value,
                    Width = borderBottom.Element(ReportItem.Namespace + "Width")?.Value
                };
            }

            PaddingLeft = style?.Element(ReportItem.Namespace + "PaddingLeft")?.Value;
            PaddingRight = style?.Element(ReportItem.Namespace + "PaddingRight")?.Value;
            PaddingTop = style?.Element(ReportItem.Namespace + "PaddingTop")?.Value;
            PaddingBottom = style?.Element(ReportItem.Namespace + "PaddingBottom")?.Value;
            BackgroundColor = style?.Element(ReportItem.Namespace + "BackgroundColor")?.Value;
            VerticalAlign = style?.Element(ReportItem.Namespace + "VerticalAlign")?.Value;
            Top = style?.Element(ReportItem.Namespace + "Top")?.Value;
            Left = style?.Element(ReportItem.Namespace + "Left")?.Value;
            Height = style?.Element(ReportItem.Namespace + "Height")?.Value;
            Width = style?.Element(ReportItem.Namespace + "Width")?.Value;
            ZIndex = style?.Element(ReportItem.Namespace + "ZIndex")?.Value;
            FontFamily = style?.Element(ReportItem.Namespace + "FontFamily")?.Value;
            FontWeight = style?.Element(ReportItem.Namespace + "FontWeight")?.Value;
            Color = style?.Element(ReportItem.Namespace + "Color")?.Value;
        }

        public string Build()
        {
            var sb = new StringBuilder();

            sb.Append("style=\"");
            sb.Append(!string.IsNullOrEmpty(TextAlign) ? $"text-align: {TextAlign.ToLower()};" : "");

            if (Border != null)
            {
                sb.Append(Border.Style == "None" ? "border: none;" : $"border: {Border.Width} solid {Border.Color};");
            }

            if (BorderBottom != null)
            {
                sb.Append(BorderBottom.Style == "None" ? "border: none;" : $"border-bottom: {BorderBottom.Width} solid {BorderBottom.Color};");
            }

            sb.Append(!string.IsNullOrEmpty(PaddingLeft) ? $"padding-left: {PaddingLeft};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingRight) ? $"padding-right: {PaddingRight};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingTop) ? $"padding-top: {PaddingTop};" : "");
            sb.Append(!string.IsNullOrEmpty(PaddingBottom) ? $"padding-bottom: {PaddingBottom};" : "");
            sb.Append(!string.IsNullOrEmpty(BackgroundColor) ? $"background-color: {BackgroundColor};" : "");

            if (!string.IsNullOrEmpty(VerticalAlign))
            {
                sb.Append(!string.IsNullOrEmpty(VerticalAlign) ? $"vertical-align: {VerticalAlign};" : "");

                switch(VerticalAlign)
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
                        
            sb.Append(!string.IsNullOrEmpty(Top) ? $"top: {Top};" : "");
            sb.Append(!string.IsNullOrEmpty(Left) ? $"left: {Left};" : "");
            sb.Append(!string.IsNullOrEmpty(Height) ? $"height: {Height};" : "");
            sb.Append(!string.IsNullOrEmpty(Width) ? $"width: {Width};" : "");
            sb.Append(!string.IsNullOrEmpty(ZIndex) ? $"z-index: {ZIndex};" : "");
            sb.Append(!string.IsNullOrEmpty(FontFamily) ? $"font-family: {FontFamily};" : "");
            sb.Append(!string.IsNullOrEmpty(FontWeight) ? $"font-weight: {FontWeight};" : "");
            sb.Append(!string.IsNullOrEmpty(Color) ? $"color: {Color};" : "");

            //if (!string.IsNullOrEmpty(this.Top) || !string.IsNullOrEmpty(this.Left))
            //{
            //    sb.Append("position: absolute;");
            //}

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
