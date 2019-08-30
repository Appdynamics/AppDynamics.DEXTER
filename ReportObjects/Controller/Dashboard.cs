using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class Dashboard
    {
        public string Controller { get; set; }

        public string DashboardName { get; set; }
        public string Description { get; set; }

        public int NumWidgets { get; set; }
        public int NumAnalyticsWidgets { get; set; }
        public int NumEventListWidgets { get; set; }
        public int NumGaugeWidgets { get; set; }
        public int NumGraphWidgets { get; set; }
        public int NumHealthListWidgets { get; set; }
        public int NumIFrameWidgets { get; set; }
        public int NumImageWidgets { get; set; }
        public int NumMetricLabelWidgets { get; set; }
        public int NumPieWidgets { get; set; }
        public int NumTextWidgets { get; set; }

        public string CanvasType { get; set; }
        public string TemplateEntityType { get; set; }
        public string SecurityToken { get; set; }
        public bool IsShared { get; set; }
        public bool IsSharingRevoked { get; set; }
        public bool IsTemplate { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public string BackgroundColor { get; set; }

        public int MinutesBefore { get; set; }
        public int RefreshInterval { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime EndTimeUtc { get; set; }

        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOn { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public long DashboardID { get; set; }
        public string DashboardLink { get; set; }

        public override String ToString()
        {
            return String.Format(
                "Dashboard: {0}/{1} ({2}) {3} widgets",
                this.Controller,
                this.DashboardName,
                this.CanvasType,
                this.NumWidgets);
        }
    }
}