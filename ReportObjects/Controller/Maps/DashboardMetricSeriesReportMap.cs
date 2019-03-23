using AppDynamics.Dexter.ReportObjects;
using CsvHelper.Configuration;

namespace AppDynamics.Dexter.ReportObjectMaps
{
    public class DashboardMetricSeriesReportMap : ClassMap<DashboardMetricSeries>
    {
        public DashboardMetricSeriesReportMap()
        {
            int i = 0;
            Map(m => m.Controller).Index(i); i++;
            Map(m => m.DashboardName).Index(i); i++;
            Map(m => m.CanvasType).Index(i); i++;
            Map(m => m.WidgetType).Index(i); i++;
            Map(m => m.Index).Index(i); i++;

            Map(m => m.SeriesName).Index(i); i++;
            Map(m => m.SeriesType).Index(i); i++;
            Map(m => m.MetricType).Index(i); i++;
            Map(m => m.Colors).Index(i); i++;
            Map(m => m.NumColors).Index(i); i++;
            Map(m => m.Axis).Index(i); i++;

            Map(m => m.MetricExpressionType).Index(i); i++;
            Map(m => m.MetricPath).Index(i); i++;
            Map(m => m.MetricDisplayName).Index(i); i++;
            Map(m => m.FunctionType).Index(i); i++;

            Map(m => m.ExpressionOperator).Index(i); i++;
            Map(m => m.Expression1).Index(i); i++;
            Map(m => m.Expression2).Index(i); i++;

            Map(m => m.MaxResults).Index(i); i++;

            Map(m => m.ApplicationName).Index(i); i++;
            Map(m => m.Expression).Index(i); i++;
            Map(m => m.EvalScopeType).Index(i); i++;
            Map(m => m.Baseline).Index(i); i++;
            Map(m => m.DisplayStyle).Index(i); i++;
            Map(m => m.DisplayFormat).Index(i); i++;

            Map(m => m.IsRollup).Index(i); i++;
            Map(m => m.UseActiveBaseline).Index(i); i++;
            Map(m => m.SortDirection).Index(i); i++;

            Map(m => m.EntityType).Index(i); i++;
            Map(m => m.EntitySelectionType).Index(i); i++;
            Map(m => m.AgentType).Index(i); i++;
            Map(m => m.SelectedEntities).Index(i); i++;
            Map(m => m.NumSelectedEntities).Index(i); i++;

            Map(m => m.IsSummary).Index(i); i++;

            Map(m => m.DashboardID).Index(i); i++;
        }
    }
}