using System;
using System.Collections.Generic;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BIQMetric : BIQEntityBase
    {
        public const string ENTITY_TYPE = "SavedSearch";
        public const string ENTITY_FOLDER = "SAVEDSRCH";

        public string MetricName { get; set; }
        public string MetricDescription { get; set; }

        public string DataSource { get; set; }
        public string EventType { get; set; }

        public bool IsEnabled { get; set; }

        public string LastExecStatus { get; set; }
        public int LastExecDuration { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }

        public string MetricLink { get; set; }
        public List<long> MetricsIDs { get; set; }
        public string MetricID { get; set; }

        public string Query { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime CreatedOnUtc { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BIQMetric: {0}/{1}({2}) {3}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID,
                this.MetricName);
        }
    }
}
