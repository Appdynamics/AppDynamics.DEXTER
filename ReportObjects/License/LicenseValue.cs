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

        public long Min { get; set; }
        public long Max { get; set; }
        public long Average { get; set; }
        public long Total { get; set; }
        public long Samples { get; set; }

        public override String ToString()
        {
            return String.Format(
                "LicenseValue: {0}/{1} {2} {3:o}={4}",
                this.Controller,
                this.AccountName,
                this.AgentType,
                this.LicenseEventTime,
                this.Average);
        }
    }
}
