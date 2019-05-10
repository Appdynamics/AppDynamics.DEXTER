using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class LicenseRule
    {
        public string Controller { get; set; }

        public long AccountID { get; set; }
        public string AccountName { get; set; }

        public string RuleName { get; set; }
        public string RuleID { get; set; }

        public string AccessKey { get; set; }

        public string AgentType { get; set; }

        public long RuleLicenses { get; set; }
        public long RulePeak { get; set; }
        public long Licenses { get; set; }

        public int NumApplications { get; set; }
        public int NumServers { get; set; }

        public int Duration { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public LicenseRule Clone()
        {
            return (LicenseRule)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "LicenseRule: {0}/{1} {2} {3}/{4}",
                this.Controller,
                this.AccountName,
                this.RuleName,
                this.RuleLicenses,
                this.Licenses);
        }
    }
}
