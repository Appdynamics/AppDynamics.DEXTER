using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class LicenseValue
    {
        public string Controller { get; set; }

        public long AccountID { get; set; }
        public string AccountName { get; set; }

        public string RuleName { get; set; }

        public string AgentType { get; set; }

        public DateTime LicenseEventTime { get; set; }
        public DateTime LicenseEventTimeUtc { get; set; }

        public long Value { get; set; }

        public override String ToString()
        {
            return String.Format(
                "MetricValue: {0}/{1} {2:o} {3}",
                this.Controller,
                this.AccountName,
                this.LicenseEventTime,
                this.Value);
        }
    }
}
