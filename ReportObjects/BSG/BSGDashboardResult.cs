using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGDashboardResult
    {
        public string Controller { get; set; }

        public string DashboardName { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public DateTime LastModifiedOnUtc { get; set; }
        public int DaysSince_LastUpdate { get; set; }
        public bool HasAnalyticsWidgets { get; set; }
        public int NumWidgets { get; set; }
        public int NumAnalyticsWidgets { get; set; }

        public BSGDashboardResult Clone()
        {
            return (BSGDashboardResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGDashboard:  {0}/{1}/{2}",
                this.Controller,
                this.DashboardName,
                this.LastModifiedOn);
        }
    }
}