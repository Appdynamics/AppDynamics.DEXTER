using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DashboardWidgetReportMap : ClassMap<DashboardWidget>
    {
        public DashboardWidgetReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.DashboardName).Index(i); i++;
            Map(m => m.CanvasType).Index(i); i++;
            Map(m => m.WidgetType).Index(i); i++;
            Map(m => m.Index).Index(i); i++;

            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntitySelectionType).Index(i); i++;
            Map(m => m.SelectedEntities).Index(i); i++;
            Map(m => m.NumSelectedEntities).Index(i); i++;

            Map(m => m.Title).Index(i); i++;
            Map(m => m.Description).Index(i); i++;
            Map(m => m.Label).Index(i); i++;
            Map(m => m.Text).Index(i); i++;
            Map(m => m.TextAlign).Index(i); i++;

            Map(m => m.Width).Index(i); i++;
            Map(m => m.Height).Index(i); i++;
            Map(m => m.MinWidth).Index(i); i++;
            Map(m => m.MinHeight).Index(i); i++;
            Map(m => m.X).Index(i); i++;
            Map(m => m.Y).Index(i); i++;

            Map(m => m.ForegroundColor).Index(i); i++;
            Map(m => m.BackgroundColor).Index(i); i++;
            Map(m => m.BackgroundAlpha).Index(i); i++;

            Map(m => m.BorderColor).Index(i); i++;
            Map(m => m.BorderSize).Index(i); i++;
            Map(m => m.IsBorderEnabled).Index(i); i++;
            Map(m => m.Margin).Index(i); i++;

            Map(m => m.NumDataSeries).Index(i); i++;

            Map(m => m.FontSize).Index(i); i++;

            Map(m => m.MinutesBeforeAnchor).Index(i); i++;

            Map(m => m.VerticalAxisLabel).Index(i); i++;
            Map(m => m.HorizontalAxisLabel).Index(i); i++;
            Map(m => m.AxisType).Index(i); i++;
            Map(m => m.IsMultipleYAxis).Index(i); i++;
            Map(m => m.StackMode).Index(i); i++;

            Map(m => m.AggregationType).Index(i); i++;

            Map(m => m.DrillDownURL).Index(i); i++;
            Map(m => m.IsDrillDownMetricBrowser).Index(i); i++;

            Map(m => m.IsShowEvents).Index(i); i++;
            Map(m => m.EventFilter).Index(i); i++;

            Map(m => m.ImageURL).Index(i); i++;
            Map(m => m.EmbeddedImageSize).Index(i); i++;

            Map(m => m.SourceURL).Index(i); i++;
            Map(m => m.IsSandbox).Index(i); i++;

            Map(m => m.AnalyticsQueries).Index(i); i++;
            Map(m => m.AnalyticsWidgetType).Index(i); i++;
            Map(m => m.AnalyticsSearchMode).Index(i); i++;

            Map(m => m.DashboardID).Index(i); i++;
        }
    }
}