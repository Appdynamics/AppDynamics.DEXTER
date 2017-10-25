using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class CustomExitRule
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
        public string MatchClass { get; set; }
        public string MatchMethod { get; set; }
        public string MatchType { get; set; }
        public string MatchParameterTypes { get; set; }
        public string DataCollectorsConfig { get; set; }
        public string InfoPointsConfig { get; set; }
        public bool IsApplyToAllBTs { get; set; }

        public int NumDetectedBackends { get; set; }
        public string DetectedBackends { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "CustomExitRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.ExitType,
                this.RuleName);
        }
    }
}
