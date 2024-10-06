using System.Xml.Linq;

namespace ReportViewer.NET.DataObjects
{
    public class ActionInfo
    {
        // XML suggests "Actions" is a collection of "Action" elements. Unsure how this would work.
        public Action Action { get; private set; }

        public ActionInfo(XElement actionInfo, ReportRDL report)
        {
            var action = actionInfo.Element(report.Namespace + "Actions").Element(report.Namespace + "Action");

            if (action != null)
            {
                this.Action = new Action(action, report);
            }
        }
    }

    public class Action
    {
        public string Hyperlink { get; private set; }
        public ActionType Type { get; private set; }

        public Action(XElement action, ReportRDL report)
        {
            var hyperlink = action.Element(report.Namespace + "Hyperlink");

            if (hyperlink != null)
            {
                this.Type = ActionType.Hyperlink;
                this.Hyperlink = hyperlink?.Value;
            }


        }                
    }

    public enum ActionType
    {
        None,
        Label,
        Hyperlink,
        Drillthrough,
        Instance,
        BookmarkLink
    }
}
