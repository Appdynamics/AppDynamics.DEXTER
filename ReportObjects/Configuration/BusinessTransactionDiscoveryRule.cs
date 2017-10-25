using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionDiscoveryRule
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        public bool IsMonitoringEnabled { get; set; }
        public string DiscoveryType { get; set; }
        public bool IsDiscoveryEnabled { get; set; }
        public string NamingConfigType { get; set; }

        public string RuleRawValue { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionDiscoveryRule: {0}/{1}/{2} {3} {4}",
                this.Controller,
                this.ApplicationName,
                this.TierName,
                this.AgentType,
                this.EntryPointType);
        }
    }
}
