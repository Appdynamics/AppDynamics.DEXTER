using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class LicenseRuleScope
    {
        public string Controller { get; set; }

        public long AccountID { get; set; }
        public string AccountName { get; set; }

        public string RuleName { get; set; }
        public string RuleID { get; set; }

        public string ScopeSelector { get; set; }

        public string MatchType { get; set; }
        public string EntityName { get; set; }

        public string EntityType { get; set; }
        public long EntityID { get; set; }

        public LicenseRuleScope Clone()
        {
            return (LicenseRuleScope)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "LicenseRuleScope: {0}/{1} {2} {3}",
                this.Controller,
                this.AccountName,
                this.RuleName,
                this.EntityName);
        }
    }
}
