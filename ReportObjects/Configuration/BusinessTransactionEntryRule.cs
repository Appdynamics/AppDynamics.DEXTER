using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionEntryRule
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        public string RuleName { get; set; }
        public bool IsBackground { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsExcluded { get; set; }
        public bool IsExclusion { get; set; }
        public int Priority { get; set; }
        public string MatchClass { get; set; }
        public string MatchMethod { get; set; }
        public string MatchURI { get; set; }
        public string SplitConfig { get; set; }
        public string Parameters { get; set; }

        public int NumDetectedBTs { get; set; }
        public string DetectedBTs { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionEntryRule: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType, 
                this.RuleName);
        }
    }
}
