using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class HealthRuleViolationEvent
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public long HealthRuleID { get; set; }
        public string HealthRuleName { get; set; }
        public string HealthRuleLink { get; set; }

        public long EntityID { get; set; }
        public string EntityType { get; set; }
        public string EntityName { get; set; }
        public string EntityLink { get; set; }

        public long EventID { get; set; }
        public DateTime From { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime To { get; set; }
        public DateTime ToUtc { get; set; }

        public string Status { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }
        public string EventLink { get; set; }

        public Event Clone()
        {
            return (Event)this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "HealthRuleViolation: {0}: {1} {2:o}",
                this.EventID,
                this.Severity,
                this.From);
        }
    }
}
