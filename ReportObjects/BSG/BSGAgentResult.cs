using System;

namespace AppDynamics.Dexter.ReportObjects
{
    public class BSGAgentResult
    {
        public string Controller { get; set; }

        public long ApplicationID { get; set; }
        public string ApplicationName { get; set; }

        public string TierName { get; set; }

        public string NodeName { get; set; }
        
        public string AgentVersion { get; set; }
        public bool IsAPMAgentUsed { get; set; }
        
        public string MachineAgentVersion { get; set; }
        public bool IsMachineAgentUsed { get; set; }

        public BSGAgentResult Clone()
        {
            return (BSGAgentResult) this.MemberwiseClone();
        }

        public override String ToString()
        {
            return String.Format(
                "BSGAgentResult: {0}/{1}({2}) {3}={4}",
                this.Controller,
                this.ApplicationName,
                this.ApplicationID);
        }
    }
}