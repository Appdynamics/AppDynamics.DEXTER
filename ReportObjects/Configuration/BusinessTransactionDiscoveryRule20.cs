using System;

namespace AppDynamics.Dexter.DataObjects
{
    public class BusinessTransactionDiscoveryRule20
    {
        public string Controller { get; set; }
        public string ControllerLink { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationLink { get; set; }
        public string ApplicationName { get; set; }

        public string ScopeName { get; set; }

        public string AgentType { get; set; }
        public string EntryPointType { get; set; }
        public string RuleName { get; set; }
        public string Description { get; set; }
        public int Version { get; set; }

        public bool IsEnabled { get; set; }
        public int Priority { get; set; }

        public bool IsMonitoringEnabled { get; set; }
        public bool IsDiscoveryEnabled { get; set; }
        public string NamingConfigType { get; set; }
        public string HTTPAutoDiscovery { get; set; }

        public override String ToString()
        {
            return String.Format(
                "BusinessTransactionDiscoveryRule20: {0}/{1}/{2} {3} {4} {5}",
                this.Controller,
                this.ApplicationName,
                this.ScopeName,
                this.AgentType,
                this.EntryPointType,
                this.RuleName);
        }
    }
}
