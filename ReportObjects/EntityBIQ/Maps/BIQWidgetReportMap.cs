using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class BIQWidgetReportMap : ClassMap<BIQWidget>
    {
        public BIQWidgetReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.ApplicationName).Index(i); i++;

            Map(m => m.SearchName).Index(i); i++;
            Map(m => m.SearchType).Index(i); i++;
            Map(m => m.SearchMode).Index(i); i++;

            Map(m => m.WidgetName).Index(i); i++;
            Map(m => m.InternalName).Index(i); i++;
            Map(m => m.WidgetType).Index(i); i++;
            Map(m => m.LegendLayout).Index(i); i++;
            Map(m => m.DataSource).Index(i); i++;

            Map(m => m.Query).Index(i); i++;

            Map(m => m.Resolution).Index(i); i++;

            Map(m => m.Width).Index(i); i++;
            Map(m => m.Height).Index(i); i++;
            Map(m => m.MinWidth).Index(i); i++;
            Map(m => m.MinHeight).Index(i); i++;
            Map(m => m.Column).Index(i); i++;
            Map(m => m.Row).Index(i); i++;

            Map(m => m.IsStacking).Index(i); i++;
            Map(m => m.IsDrilledDown).Index(i); i++;

            Map(m => m.FontSize).Index(i); i++;

            Map(m => m.Color).Index(i); i++;
            Map(m => m.BackgroundColor).Index(i); i++;

            Map(m => m.TimeRangeType).Index(i); i++;
            Map(m => m.TimeRangeDuration).Index(i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.StartTime), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.StartTimeUtc), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.EndTime), i); i++;
            EPPlusCSVHelper.setISO8601DateFormat(Map(m => m.EndTimeUtc), i); i++;

            Map(m => m.ApplicationID).Index(i); i++;
            Map(m => m.SearchID).Index(i); i++;
            Map(m => m.WidgetID).Index(i); i++;
            Map(m => m.ControllerLink).Index(i); i++;
            Map(m => m.ApplicationLink).Index(i); i++;
            Map(m => m.SearchLink).Index(i); i++;
        }
    }
}