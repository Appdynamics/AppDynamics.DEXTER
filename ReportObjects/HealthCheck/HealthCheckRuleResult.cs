using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HealthCheckRuleResult
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string Application { get; set; }

        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public long EntityID { get; set; }

        public string Category { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int Grade { get; set; }
        public string Description { get; set; }

        public DateTime EvaluationTime { get; set; }
        public string Version { get; set; }

        public string RuleLink { get; set; }

        public HealthCheckRuleResult Clone()
        {
            return (HealthCheckRuleResult)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "HealthCheckRuleResult: {0}/{1}({2}) {3}={4}",
                this.Controller,
                this.Application,
                this.ApplicationID,
                this.Name,
                this.Grade);
        }
    }
}
