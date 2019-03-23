using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class PolicyActionMapping : ConfigurationEntityBase
    {
        public string PolicyName { get; set; }
        public string PolicyType { get; set; }

        public string ActionName { get; set; }
        public string ActionType { get; set; }

        public long PolicyID { get; set; }
        public long ActionID { get; set; }

        public override string EntityIdentifier
        {
            get
            {
                return String.Format("{0}/{1}", this.PolicyName, this.ActionName);
            }
        }

        public override string EntityName
        {
            get
            {
                return this.PolicyName;
            }
        }

        public override string RuleType
        {
            get
            {
                return "PolicyActionMapping";
            }
        }

        public override string RuleSubType
        {
            get
            {
                return this.ActionName;
            }
        }

        public override String ToString()
        {
            return String.Format(
                "PolicyActionMapping: {0}/{1}/{2}/{3}",
                this.Controller,
                this.ApplicationName,
                this.PolicyName,
                this.ActionName);
        }
    }
}
