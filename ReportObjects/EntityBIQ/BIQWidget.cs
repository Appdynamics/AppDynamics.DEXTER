using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQWidget : BIQEntityBase
    {
        public string SearchName { get; set; }
        public string SearchType { get; set; }
        public string SearchMode { get; set; }
        public long SearchID { get; set; }
        public string SearchLink { get; set; }

        public string WidgetName { get; set; }
        public string WidgetType { get; set; }
        public string InternalName { get; set; }
        public string LegendLayout { get; set; }
        public string DataSource { get; set; }
        public long WidgetID { get; set; }

        public string Query { get; set; }

        public string Resolution { get; set; }

        public string TimeRangeType { get; set; }
        public int TimeRangeDuration { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime EndTimeUtc { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public int MinWidth { get; set; }
        public int MinHeight { get; set; }

        public int Column { get; set; }
        public int Row { get; set; }

        public bool IsStacking { get; set; }
        public bool IsDrilledDown { get; set; }
        public int FontSize { get; set; }

        public string BackgroundColor { get; set; }

        public string Color { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQWidget: {0}/{1}({2}) {3}[{4}]({5}) {6}[{7}]({8})",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.SearchName,
                this.SearchType,
                this.SearchID,
                this.WidgetName, 
                this.WidgetType,
                this.WidgetID);
        }
    }
}
