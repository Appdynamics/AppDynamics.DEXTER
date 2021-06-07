using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGHealthRuleResult
    {
        public string Controller { get; set; }
        
        public string ApplicationName { get; set; }

        public int WarningViolations { get; set; }
        public int CriticalViolations { get; set; }
        public int DefaultHealthRulesModified { get; set; }
        public int CustomHealthRules { get; set; }
        public int LinkedPolicies { get; set; }
        public int LinkedActions { get; set; }
        

        public BSGHealthRuleResult Clone()
        {
            return (BSGHealthRuleResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGHealthRuleResult: {0}/{1}:({2})",
                this.Controller,
                this.ApplicationName);
        }
    }
}