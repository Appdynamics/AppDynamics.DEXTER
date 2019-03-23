using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class DashboardMetricSeries
    {
        public string Controller { get; set; }
        public string DashboardName { get; set; }
        public string CanvasType { get; set; }
        public string WidgetType { get; set; }
        public int Index { get; set; }

        public string SeriesName { get; set; }
        public string SeriesType { get; set; }
        public string MetricType { get; set; }
        public string Colors { get; set; }
        public int NumColors { get; set; }
        public string Axis { get; set; }

        public string MetricExpressionType { get; set; }
        public string MetricPath { get; set; }
        public string MetricDisplayName { get; set; }
        public string FunctionType { get; set; }

        public string ExpressionOperator { get; set; }
        public string Expression1 { get; set; }
        public string Expression2 { get; set; }

        public int MaxResults { get; set; }

        public string ApplicationName { get; set; }
        public string Expression { get; set; }
        public string EvalScopeType { get; set; }
        public string Baseline { get; set; }
        public string DisplayStyle { get; set; }
        public string DisplayFormat { get; set; }

        public bool IsRollup { get; set; }
        public bool UseActiveBaseline { get; set; }
        public string SortDirection { get; set; }

        public string EntityType { get; set; }
        public string EntitySelectionType { get; set; }
        public string AgentType { get; set; }
        public string SelectedEntities { get; set; }
        public int NumSelectedEntities { get; set; }

        public bool IsSummary { get; set; }

        public long DashboardID { get; set; }

        public override String ToString()
        {
            return String.Format(
                "DashboardMetricSeries: {0}/{1} {2} {3} {4}",
                this.Controller, 
                this.DashboardName,
                this.WidgetType,
                this.Index,
                this.SeriesName);
        }
    }
}
