using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DashboardWidget
    {
        public string Controller { get; set; }
        public string DashboardName { get; set; }
        public string CanvasType { get; set; }
        public string WidgetType { get; set; }
        public int Index { get; set; }

        public string ApplicationName { get; set; }
        public string EntityType { get; set; }
        public string EntitySelectionType { get; set; }
        public string SelectedEntities { get; set; }
        public int NumSelectedEntities { get; set; }

        public string Title { get; set; }
        public string Description { get; set; }
        public string Label { get; set; }
        public string Text { get; set; }
        public string TextAlign { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
        public int MinWidth { get; set; }
        public int MinHeight { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public string ForegroundColor { get; set; }
        public string BackgroundColor { get; set; }
        public double BackgroundAlpha { get; set; }

        public string BorderColor { get; set; }
        public int BorderSize { get; set; }
        public bool IsBorderEnabled { get; set; }
        public int Margin { get; set; }

        public int NumDataSeries { get; set; }

        public int FontSize { get; set; }

        public int MinutesBeforeAnchor { get; set; }

        public string VerticalAxisLabel { get; set; }
        public string HorizontalAxisLabel { get; set; }
        public string AxisType { get; set; }
        public bool IsMultipleYAxis { get; set; }
        public string StackMode { get; set; }

        public string AggregationType { get; set; }

        public string DrillDownURL { get; set; }
        public bool IsDrillDownMetricBrowser { get; set; }

        public bool IsShowEvents { get; set; }
        public string EventFilter { get; set; }

        public string ImageURL { get; set; }
        public int EmbeddedImageSize { get; set; }

        public string SourceURL { get; set; }
        public bool IsSandbox { get; set; }

        public string AnalyticsQueries { get; set; }
        public string AnalyticsWidgetType { get; set; }
        public string AnalyticsSearchMode { get; set; }

        public long DashboardID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DashboardWidget: {0}/{1} {2} {3}x{4}",
                this.Controller,
                this.DashboardName,
                this.WidgetType,
                this.Width,
                this.Height);
        }
    }
}
