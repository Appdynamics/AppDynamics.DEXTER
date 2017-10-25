using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class BackendDiscoveryRule
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public string AgentType { get; set; }
        public string ExitType { get; set; }
        public string RuleName { get; set; }
        public bool IsCorrelationSupported { get; set; }
        public bool IsCorrelationEnabled { get; set; }
        public bool IsEnabled { get; set; }
        public int Priority { get; set; }
        public string IdentityOptions { get; set; }
        public string DiscoveryConditions { get; set; }

        public int NumDetectedBackends { get; set; }
        public string DetectedBackends { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BackendDiscoveryRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.ExitType,
                this.RuleName);
        }
    }
}
