using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class InformationPointRule
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string AgentType { get; set; }
        public string RuleName { get; set; }
        public string MatchClass { get; set; }
        public string MatchMethod { get; set; }
        public string MatchType { get; set; }
        public string MatchParameterTypes { get; set; }
        public string MatchCondition { get; set; }
        public string InfoPointsConfig { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "InformationPointRule: {0}/{1}/{2} {3}",
                this.Controller,
                this.ApplicationName,
                this.AgentType,
                this.RuleName);
        }
    }
}
